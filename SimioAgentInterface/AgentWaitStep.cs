using NetMQ;
using SimioAPI;
using SimioAPI.Extensions;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SimioAgentInterface
{
    class AgentWaitDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces.
        /// </summary>
        public string Name
        {
            get { return "AgentWait"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.
        /// </summary>
        public string Description
        {
            get { return "Wait for the agent to ask for an action"; }
        }

        /// <summary>
        /// Property returning an icon to display for the step in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the step.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{35e5b47a-be6f-495d-b3ed-7be3fc2312ef}");

        /// <summary>
        /// Property returning the number of exits out of the step. Can return either 1 or 2.
        /// </summary>
        public int NumberOfExits
        {
            get { return 2; }
        }

        /// <summary>
        /// Method called that defines the property schema for the step.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            var pd = schema.AddElementProperty("AgentConnection", AgentConnectionDefinition.MY_ID);
            pd.DisplayName = "Agent Connection";
            pd.Description = "An agent connection object.";
            pd.Required = true;


            pd = schema.AddStateProperty("EpisodeNumber");
            pd.DisplayName = "Episode Number";
            pd.Description = "The number of the episode the agent is currently in";
            pd.Required = true;

            pd = schema.AddStateProperty("Status");
            pd.DisplayName = "Agent Status";
            pd.Description = "0: Ongoing, 1: Failure, 2: Success";
            pd.Required = true;

            pd = schema.AddStateProperty("Reward");
            pd.DisplayName = "Reward";
            pd.Description = "The reward given to the agent as a consequence of the last action taken";
            pd.Required = true;

            pd = schema.AddStateProperty("Action");
            pd.DisplayName = "Action";
            pd.Description = "The actions received from the agent";
            pd.Required = true;

            IRepeatGroupPropertyDefinition parts = schema.AddRepeatGroupProperty("States");
            parts.DisplayName = "States";
            parts.Description = "The states that will be given to the agent";
            parts.Required = true;
            pd = parts.PropertyDefinitions.AddStateProperty("State");
            pd.Description = "State";
        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process.
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new AgentWaitStep(properties);
        }

        #endregion
    }

    class AgentWaitStep : IStep
    {
        IPropertyReaders _properties;
        IElementProperty _agentProperty;
        IRepeatingPropertyReader _statesProperty;
        IStateProperty _actionProperty;
        IStateProperty _rewardProperty;
        IStateProperty _episodeProperty;
        IStateProperty _statusProperty;

        public AgentWaitStep(IPropertyReaders properties)
        {
            _properties = properties;
            _agentProperty = (IElementProperty)_properties.GetProperty("AgentConnection");
            _statesProperty = (IRepeatingPropertyReader)_properties.GetProperty("States");
            _actionProperty = (IStateProperty)_properties.GetProperty("Action");
            _rewardProperty = (IStateProperty)_properties.GetProperty("Reward");
            _episodeProperty = (IStateProperty)_properties.GetProperty("EpisodeNumber");
            _statusProperty = (IStateProperty)_properties.GetProperty("Status");
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {

            // Prepare Message
            //=================
            AgentConnection agentConnection = (AgentConnection)_agentProperty.GetElement(context);
            var socket = agentConnection.GetSocket();
            var states = ReadRepeatingProperty(context, _statesProperty, "State");
            var action = _actionProperty.GetState(context).StateValue;
            var reward = _rewardProperty.GetState(context).StateValue;
            var episode = _episodeProperty.GetState(context).StateValue;
            var status = _statusProperty.GetState(context).StateValue;

            var requestMessage = new AgentRequestMessage()
            {
                Status = status,
                EpisodeNumber = episode,
                Reward = reward,
                States = states
            };

            // Send Request Message to Agent
            //===============================
            var message = JsonConvert.SerializeObject(requestMessage);
            bool result = socket.TrySendFrame(TimeSpan.FromSeconds(3), message);
            if (!result)
            {
                context.ExecutionInformation.ReportError("Failed to communicate with agent");
            }

            // Wait For Response Message From Agent
            //======================================
            string responseMessage;
            result = socket.TryReceiveFrameString(TimeSpan.FromSeconds(3), out responseMessage);
            if (!result)
            {
                context.ExecutionInformation.ReportError("Failed to communicate with agent");
            }
            var jsonresponse = JsonConvert.DeserializeObject<AgentResponseMessage>(responseMessage);


            // Return Action Value
            //======================================
            context.ExecutionInformation.TraceInformation(responseMessage);
            (_actionProperty.GetState(context) as IRealState).Value = jsonresponse.Action;
            context.ReturnValue = jsonresponse.Action;

            return (jsonresponse.IsNoOp) ? ExitType.AlternateExit : ExitType.FirstExit;
            // ExitType.FirstExit;
        }


        private List<double> ReadRepeatingProperty(IStepExecutionContext context, IRepeatingPropertyReader rp, string propertyname)
        {
            var result = new List<double>();
            for (int i = 0; i < rp.GetCount(context); i++)
            {
                using (IPropertyReaders row = rp.GetRow(i, context))
                {
                    IStateProperty stateprop = (IStateProperty)row.GetProperty(propertyname);
                    IState state = stateprop.GetState(context);
                    result.Add(state.StateValue);
                }
            }
            return result;
        }

        #endregion
    }
}
