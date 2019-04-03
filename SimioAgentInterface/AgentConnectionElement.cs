using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimioAPI;
using SimioAPI.Extensions;
using NetMQ.Sockets;


namespace SimioAgentInterface
{
    class AgentConnectionDefinition : IElementDefinition
    {
        #region IElementDefinition Members

        /// <summary>
        /// Property returning the full name for this type of element. The name should contain no spaces.
        /// </summary>
        public string Name
        {
            get { return "AgentConnection"; }
        }

        /// <summary>
        /// Property returning a short description of what the element does.
        /// </summary>
        public string Description
        {
            get { return "Connection object to an external agent program."; }
        }

        /// <summary>
        /// Property returning an icon to display for the element in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the element.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        public static readonly Guid MY_ID = new Guid("{010b68f4-657c-4445-9b8e-4cf3127e2970}");

        /// <summary>
        /// Method called that defines the property, state, and event schema for the element.
        /// </summary>
        public void DefineSchema(IElementSchema schema)
        {
            // Example of how to add a property definition to the element.
            IPropertyDefinition pd;

            pd = schema.PropertyDefinitions.AddRealProperty("Port", 5000);
            pd.DisplayName = "Port";
            pd.Description = "The port number to use for the agent connection";
            pd.Required = true;
        }

        /// <summary>
        /// Method called to add a new instance of this element type to a model.
        /// Returns an instance of the class implementing the IElement interface.
        /// </summary>
        public IElement CreateElement(IElementData data)
        {
            return new AgentConnection(data);
        }

        #endregion
    }

    class AgentConnection : IElement
    {
        IElementData _data;
        RequestSocket _requestSocket;
        int _port = 0;
        public AgentConnection(IElementData data)
        {
            _data = data;
            _port = Convert.ToInt32(_data.Properties.GetProperty("Port").GetDoubleValue(data.ExecutionContext));
            _requestSocket = new RequestSocket();
        }

        public RequestSocket GetSocket()
        {
            return _requestSocket;
        }

        #region IElement Members

        /// <summary>
        /// Method called when the simulation run is initialized.
        /// </summary>
        public void Initialize()
        {
            _requestSocket.Bind($"tcp://localhost:{_port}");
        }

        /// <summary>
        /// Method called when the simulation run is terminating.
        /// </summary>
        public void Shutdown()
        {
            _requestSocket.Close();
        }

        #endregion
    }
}
