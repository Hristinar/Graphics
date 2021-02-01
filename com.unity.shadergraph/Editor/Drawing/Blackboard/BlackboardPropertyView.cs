﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;

using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardPropertyView : GraphElement, IInspectable, ISGControlledElement<ShaderInputViewController>
    {
        static readonly Texture2D k_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        static readonly string k_UxmlTemplatePath = "UXML/GraphView/BlackboardField";
        static readonly string k_StyleSheetPath = "Styles/Blackboard";

        ShaderInputViewModel m_ViewModel;

        ShaderInputViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        VisualElement m_ContentItem;
        Pill m_Pill;
        TextField m_TextField;
        Label m_TypeLabel;

        ShaderInputPropertyDrawer.ChangeReferenceNameCallback m_ResetReferenceNameTrigger;
        List<Node> m_SelectedNodes = new List<Node>();

        public string text
        {
            get { return m_Pill.text; }
            set { m_Pill.text = value; }
        }

        public string typeText
        {
            get { return m_TypeLabel.text; }
            set { m_TypeLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Pill.icon; }
            set { m_Pill.icon = value; }
        }

        public bool highlighted
        {
            get { return m_Pill.highlighted; }
            set { m_Pill.highlighted = value; }
        }

        internal BlackboardPropertyView(ShaderInputViewModel viewModel)
        {
            ViewModel = viewModel;
            // Store ShaderInput in userData object
            userData = ViewModel.Model;

            var visualTreeAsset = Resources.Load<VisualTreeAsset>(k_UxmlTemplatePath);
            Assert.IsNotNull(visualTreeAsset);

            VisualElement mainContainer = visualTreeAsset.Instantiate();
            var styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            Assert.IsNotNull(styleSheet);
            styleSheets.Add(styleSheet);

            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");
            m_Pill = mainContainer.Q<Pill>("pill");
            m_TypeLabel = mainContainer.Q<Label>("typeLabel");
            m_TextField = mainContainer.Q<TextField>("textField");
            m_TextField.style.display = DisplayStyle.None;

            // Update the Pill text if shader input name is changed
            // we handle this in controller if we change it through BlackboardPropertyView, but its possible to change through PropertyNodeView as well
            shaderInput.displayNameUpdateTrigger += newDisplayName => text = newDisplayName;

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardField");

            this.name = "BlackboardPropertyView";
            UpdateFromViewModel();

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);
            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildFieldContextualMenu));

            // When a display name is changed through the BlackboardPill, bind this callback to handle it with appropriate change action
            var textInputElement = m_TextField.Q(TextField.textInputUssName);
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });
            textInputElement.RegisterCallback<FocusOutEvent>(e =>
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.ShaderInputReference = shaderInput;
                changeDisplayNameAction.NewDisplayNameValue = m_TextField.text;
                ViewModel.RequestModelChangeAction(changeDisplayNameAction);
            });

            ShaderGraphPreferences.onAllowDeprecatedChanged += UpdateTypeText;

            RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, ViewModel.Model));
            RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, ViewModel.Model));
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            if (ViewModel.ParentView is SGBlackboard blackboard)
            {
                // These callbacks are used for the property dragging scroll behavior
                RegisterCallback<DragEnterEvent>(evt => blackboard.ShowScrollBoundaryRegions());
                RegisterCallback<DragExitedEvent>(evt => blackboard.HideScrollBoundaryRegions());

                // These callbacks are used for the property dragging scroll behavior
                RegisterCallback<DragEnterEvent>(evt => blackboard.ShowScrollBoundaryRegions());
                RegisterCallback<DragExitedEvent>(evt => blackboard.HideScrollBoundaryRegions());
            }

        }

        ~BlackboardPropertyView()
        {
            ShaderGraphPreferences.onAllowDeprecatedChanged -= UpdateTypeText;
        }

        void AddContextMenuOptions(ContextualMenuPopulateEvent evt)
        {
            // Checks if the reference name has been overridden and appends menu action to reset it, if so
            if (shaderInput.isRenamable &&
                !string.IsNullOrEmpty(shaderInput.overrideReferenceName))
            {
                evt.menu.AppendAction(
                    "Reset Reference",
                    e =>
                    {
                        var resetReferenceNameAction = new ResetReferenceNameAction();
                        resetReferenceNameAction.ShaderInputReference = shaderInput;
                        ViewModel.RequestModelChangeAction(resetReferenceNameAction);
                        m_ResetReferenceNameTrigger(shaderInput.referenceName);
                    },
                    DropdownMenuAction.AlwaysEnabled);
            }
        }

        internal void UpdateFromViewModel()
        {
            this.text = ViewModel.InputName;
            this.icon = ViewModel.IsInputExposed ? k_ExposedIcon : null;
            this.typeText = ViewModel.InputTypeName;
        }

        ShaderInputViewController m_Controller;

        // --- Begin ISGControlledElement implementation
        public void OnControllerChanged(ref SGControllerChangedEvent e)
        {

        }

        public void OnControllerEvent(SGControllerEvent e)
        {

        }

        public ShaderInputViewController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }

                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        SGController ISGControlledElement.controller => m_Controller;

        // --- ISGControlledElement implementation

        [Inspectable("Shader Input", null)]
        ShaderInput shaderInput => ViewModel.Model;

        public string inspectorTitle => ViewModel.InputName + " " + ViewModel.InputTypeName;

        public object GetObjectToInspect()
        {
            return shaderInput;
        }

        Action m_InspectorUpdateDelegate;

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if (propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                // TODO: We currently need to do a halfway measure between the old way of handling stuff for property drawers (how FieldView and NodeView handle it)
                // and how we want to handle it with the new style of controllers and views. Ideally we'd just hand the property drawer a view model and thats it.
                // We've maintained all the old callbacks as they are in the PropertyDrawer to reduce possible halo changes and support PropertyNodeView functionality
                // Instead we supply different underlying methods for the callbacks in the new BlackboardPropertyView,
                // that way both code paths should work until we can refactor PropertyNodeView
                shaderInputPropertyDrawer.GetViewModel(ViewModel, controller.DataStoreState,
                    ((triggerInspectorUpdate, modificationScope) =>
                    {
                        controller.DirtyNodes(modificationScope);
                        if (triggerInspectorUpdate)
                            inspectorUpdateDelegate();

                    }));
                m_ResetReferenceNameTrigger = shaderInputPropertyDrawer._resetReferenceNameCallback;
                this.RegisterCallback<DetachFromPanelEvent>(evt => inspectorUpdateDelegate());

                m_InspectorUpdateDelegate = inspectorUpdateDelegate;
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                int x = 0;
                x++;
                // TODO: Re-enable somehow (going to need to grab internal function which is gross but temporary at least)
                //if (ViewModel.ParentView is GraphView graphView)
                //    graphView.RestorePersitentSelectionForElement(this);
            }
        }

        void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.style.display = DisplayStyle.None;

            if (text != m_TextField.text)
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.ShaderInputReference = shaderInput;
                changeDisplayNameAction.NewDisplayNameValue = m_TextField.text;
                ViewModel.RequestModelChangeAction(changeDisplayNameAction);
                m_InspectorUpdateDelegate?.Invoke();
            }
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        // TODO: Move to controller? Feels weird for this to be directly communicating with PropertyNodes etc.
        // Better way would be to send event to controller that notified of hover enter/exit and have other controllers be sent those events in turn
        void OnMouseHover(EventBase evt, ShaderInput input)
        {
            var graphView = ViewModel.ParentView.GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if (input is AbstractShaderProperty property)
                    {
                        if (node.userData is PropertyNode propertyNode)
                        {
                            if (propertyNode.property == input)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                    else if (input is ShaderKeyword keyword)
                    {
                        if (node.userData is KeywordNode keywordNode)
                        {
                            if (keywordNode.keyword == input)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                }
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId() && m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        void UpdateTypeText()
        {
            if (shaderInput is AbstractShaderProperty asp)
            {
                typeText = asp.GetPropertyTypeString();
            }
        }

        public void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_ContentItem.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        protected virtual void BuildFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
        }
    }
}
