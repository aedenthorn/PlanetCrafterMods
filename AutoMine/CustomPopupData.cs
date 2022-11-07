using SpaceCraft;
using UnityEngine;

namespace AutoMine
{
    internal class CustomPopupData : PopupData
    {
        public string audio;

        public CustomPopupData(Sprite icon, string text, int _displayTime, bool forceDisplayImmediate, string audio) : base(icon, text, _displayTime, forceDisplayImmediate)
        {
            this.audio = audio;
        }
    }
}