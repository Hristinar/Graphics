using System;
using UnityEditor.Graphs;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.Graphing
{
    class HandleUndoRedoAction : IGraphDataAction
    {
        void HandleGraphUndoRedo(GraphData m_GraphData)
        {
            m_GraphData?.ReplaceWith(NewGraphData);
        }

        public Action<GraphData> modifyGraphDataAction => HandleGraphUndoRedo;

        public GraphData NewGraphData { get; set; }
    }

    class GraphObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        SerializationHelper.JSONSerializedElement m_SerializedGraph;

        [SerializeField]
        int m_SerializedVersion;

        [SerializeField]
        bool m_IsDirty;

        [SerializeField]
        bool m_IsSubGraph;

        [SerializeField]
        string m_AssetGuid;

        [NonSerialized]
        GraphData m_Graph;

        [NonSerialized]
        int m_DeserializedVersion;

        public DataStore<GraphData> graphDataStore
        {
            get => m_DataStore;
            private set
            {
                if (m_DataStore != value && value != null)
                    m_DataStore = value;
            }
        }

        DataStore<GraphData> m_DataStore;

        public GraphData graph
        {
            get { return m_Graph; }
            set
            {
                if (m_Graph != null)
                    m_Graph.owner = null;
                m_Graph = value;
                graphDataStore = new DataStore<GraphData>(ReduceGraphDataAction, m_Graph);
                if (m_Graph != null)
                    m_Graph.owner = this;
            }
        }

        // this value stores whether an undo operation has been registered (which indicates a change has been made to the graph)
        // and is used to trigger the MaterialGraphEditWindow to update it's title
        public bool isDirty
        {
            get { return m_IsDirty; }
            set { m_IsDirty = value; }
        }

        public virtual void RegisterCompleteObjectUndo(string actionName)
        {
            Undo.RegisterCompleteObjectUndo(this, actionName);
            m_SerializedVersion++;
            m_DeserializedVersion++;
            m_IsDirty = true;
        }

        public void OnBeforeSerialize()
        {
            if (graph != null)
            {
                var json = MultiJson.Serialize(graph);
                m_SerializedGraph = new SerializationHelper.JSONSerializedElement { JSONnodeData = json };
                m_IsSubGraph = graph.isSubGraph;
                m_AssetGuid = graph.assetGuid;
            }
        }

        public void OnAfterDeserialize()
        {
        }

        public bool wasUndoRedoPerformed => m_DeserializedVersion != m_SerializedVersion;

        public void HandleUndoRedo()
        {
            Debug.Assert(wasUndoRedoPerformed);
            var deserializedGraph = DeserializeGraph();

            var handleUndoRedoAction = new HandleUndoRedoAction();
            handleUndoRedoAction.NewGraphData = deserializedGraph;
            graphDataStore.Dispatch(handleUndoRedoAction);
        }

        GraphData DeserializeGraph()
        {
            var json = m_SerializedGraph.JSONnodeData;
            var deserializedGraph = new GraphData {isSubGraph = m_IsSubGraph, assetGuid = m_AssetGuid};
            MultiJson.Deserialize(deserializedGraph, json);
            m_DeserializedVersion = m_SerializedVersion;
            m_SerializedGraph = default;
            return deserializedGraph;
        }

        public void Validate()
        {
            if (graph != null)
            {
                graph.OnEnable();
                graph.ValidateGraph();
            }
        }

        // This is a very simple reducer, all it does is take the action and apply it to the graph data, which causes some mutation in state
        // This isn't strictly redux anymore but its needed given that our state tree is quite large and we don't want to be creating copies of it everywhere by unboxing
        void ReduceGraphDataAction(GraphData initialState, IGraphDataAction graphDataAction)
        {
            graphDataAction.modifyGraphDataAction(initialState);
        }

        void OnEnable()
        {
            if (graph == null && m_SerializedGraph.JSONnodeData != null)
            {
                graph = DeserializeGraph();
            }
            Validate();
        }

        void OnDestroy()
        {
            graph?.OnDisable();
        }
    }
}
