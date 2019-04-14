using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Builder;
using Loxodon.Framework.ViewModels;
using Loxodon.Framework.Views;
using Assets.UiFramework.Libs;
using UnityEngine.UI;
using UIComponent.UI;

namespace App.Client.GameModules.Ui.ViewModels.Common
{
    public class CommonPlayerInfoViewModel : ViewModelBase, IUiViewModel
    {
        private class CommonPlayerInfoView : UIView
        {
            public GameObject ShowActive;
            [HideInInspector]
            public bool oriShowActive;
            
            public void FillField()
            {
                RectTransform[] gameobjects = gameObject.GetComponentsInChildren<RectTransform>(true);
                foreach (var v in gameobjects)
                {
                    var realName = v.gameObject.name.Replace("(Clone)","");
                    switch (realName)
                    {
                        case "Show":
                            ShowActive = v.gameObject;
                            break;
                    }
                }

            }
        }


        private bool _showActive;
        public bool ShowActive { get { return _showActive; } set {if(_showActive != value) Set(ref _showActive, value, "ShowActive"); } }

		private GameObject _viewGameObject;
		private Canvas _viewCanvas;
		private CommonPlayerInfoView _view;
		
		public void Destory()
        {
            if (_viewGameObject != null)
            {
				UnityEngine.Object.Destroy(_viewGameObject);
            }
        }
		public void Visible(bool isViaible)
		{
		    if (_viewGameObject != null)
            {
				_viewGameObject.SetActive(isViaible);
            }
		
		}
		public void SetCanvasEnabled(bool value)
        {
            if (_viewCanvas != null)
            {
                _viewCanvas.enabled = value;
            }
        }
        public void CreateBinding(GameObject obj)
        {
			_viewGameObject = obj;
			_viewCanvas = _viewGameObject.GetComponent<Canvas>();

			bool bFirst = false;
			var view = obj.GetComponent<CommonPlayerInfoView>();
			if(view == null)
			{
				bFirst = true;
				view = obj.AddComponent<CommonPlayerInfoView>();
				view.FillField();
			}
			DataInit(view);
			SpriteReset();
			view.BindingContext().DataContext = this;
			if(bFirst)
			{
				SaveOriData(view);
				ViewBind(view);
			}
			_view = view;

        }
		private void EventTriggerBind(CommonPlayerInfoView view)
		{
		}

        private static readonly Dictionary<string, PropertyInfo> PropertySetter = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, MethodInfo> MethodSetter = new Dictionary<string, MethodInfo>();

        static CommonPlayerInfoViewModel()
        {
            Type type = typeof(CommonPlayerInfoViewModel);
            foreach (var property in type.GetProperties())
            {
                if (property.CanWrite)
                {
                    PropertySetter.Add(property.Name, property);
                }
            }
			foreach (var methodInfo in type.GetMethods())
            {
                if (methodInfo.IsPublic)
                {
                    MethodSetter.Add(methodInfo.Name, methodInfo);
                }
            }
        }

		void ViewBind(CommonPlayerInfoView view)
		{
		     BindingSet<CommonPlayerInfoView, CommonPlayerInfoViewModel> bindingSet =
                view.CreateBindingSet<CommonPlayerInfoView, CommonPlayerInfoViewModel>();
            bindingSet.Bind(view.ShowActive).For(v => v.activeSelf).To(vm => vm.ShowActive).OneWay();
		
			bindingSet.Build();
		}

		void DataInit(CommonPlayerInfoView view)
		{
            _showActive = view.ShowActive.activeSelf;
		}


		void SaveOriData(CommonPlayerInfoView view)
		{
            view.oriShowActive = _showActive;
		}




		private void SpriteReset()
		{
		}

		public void Reset()
		{
			if(_viewGameObject == null)
			{
				return;
			}
			ShowActive = _view.oriShowActive;
			SpriteReset();
		}

		public void CallFunction(string functionName)
        {
            if (MethodSetter.ContainsKey(functionName))
            {
                MethodSetter[functionName].Invoke(this, null);
            }
        }

		public bool IsPropertyExist(string propertyId)
        {
            return PropertySetter.ContainsKey(propertyId);
        }

		public Transform GetParentLinkNode()
		{
			return null;
		}

		public Transform GetChildLinkNode()
		{
			return null;
		}

        public string ResourceBundleName { get { return "ui/client/prefab/common"; } }
        public string ResourceAssetName { get { return "CommonPlayerInfo"; } }
        public string ConfigBundleName { get { return ""; } }
        public string ConfigAssetName { get { return ""; } }
    }
}
