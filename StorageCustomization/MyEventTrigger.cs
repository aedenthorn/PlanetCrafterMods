using SpaceCraft;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace StorageCustomization
{
    public class MyEventTrigger : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
	{
		public static void AddTriggerEvent(GameObject _gameObject, EventTriggerType _triggerType, Action<EventTriggerCallbackData> _callBack, Group _group = null, WorldObject _worldObject = null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = _triggerType;
			entry.callback.AddListener(delegate (BaseEventData data)
			{
				_callBack(new EventTriggerCallbackData((PointerEventData)data, _group, _worldObject, 0));
			});
			_gameObject.AddComponent<MyEventTrigger>().triggers.Add(entry);
		}
		public List<EventTrigger.Entry> delegates
		{
			get
			{
				return this.triggers;
			}
			set
			{
				this.triggers = value;
			}
		}

		protected MyEventTrigger()
		{
		}
		
		public MyEventTrigger(EventTrigger trigger)
		{
		}

		public List<EventTrigger.Entry> triggers
		{
			get
			{
				if (this.m_Delegates == null)
				{
					this.m_Delegates = new List<EventTrigger.Entry>();
				}
				return this.m_Delegates;
			}
			set
			{
				this.m_Delegates = value;
			}
		}

		private void Execute(EventTriggerType id, BaseEventData eventData)
		{
			int count = this.triggers.Count;
			int i = 0;
			int count2 = this.triggers.Count;
			while (i < count2)
			{
				EventTrigger.Entry entry = this.triggers[i];
				if (entry.eventID == id && entry.callback != null)
				{
					entry.callback.Invoke(eventData);
				}
				i++;
			}
		}

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.PointerEnter, eventData);
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.PointerExit, eventData);
		}

		public virtual void OnDrag(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.Drag, eventData);
		}

		public virtual void OnDrop(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.Drop, eventData);
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.PointerDown, eventData);
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.PointerUp, eventData);
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.PointerClick, eventData);
		}

		public virtual void OnSelect(BaseEventData eventData)
		{
			this.Execute(EventTriggerType.Select, eventData);
		}

		public virtual void OnDeselect(BaseEventData eventData)
		{
			this.Execute(EventTriggerType.Deselect, eventData);
		}

		public virtual void OnScroll(PointerEventData eventData)
		{
			this.Execute(EventTriggerType.Scroll, eventData);
		}

		public virtual void OnMove(AxisEventData eventData)
		{
			this.Execute(EventTriggerType.Move, eventData);
		}

		public virtual void OnUpdateSelected(BaseEventData eventData)
		{
			this.Execute(EventTriggerType.UpdateSelected, eventData);
		}
		public virtual void OnSubmit(BaseEventData eventData)
		{
			this.Execute(EventTriggerType.Submit, eventData);
		}

		public virtual void OnCancel(BaseEventData eventData)
		{
			this.Execute(EventTriggerType.Cancel, eventData);
		}

		[SerializeField]
		private List<EventTrigger.Entry> m_Delegates;

		[Serializable]
		public class TriggerEvent : UnityEvent<BaseEventData>
		{
		}

		[Serializable]
		public class Entry
		{
			public EventTriggerType eventID = EventTriggerType.PointerClick;

			public EventTrigger.TriggerEvent callback = new EventTrigger.TriggerEvent();
		}
	}
}
