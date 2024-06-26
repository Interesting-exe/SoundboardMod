﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApolloCore.API;
using ApolloCore.API.QM;
using AtgDev.Voicemeeter;
using AtgDev.Voicemeeter.Utils;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI.Elements.Controls;


namespace SoundpadMod
{
    public class Main : MelonMod
    {
        private static RemoteApiWrapper _vmr;
        private static readonly string Path = $"{Directory.GetCurrentDirectory()}\\Sounds\\";
        private static string[] _files = Directory.GetFiles(Path,  ".").Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3")).ToArray();
        private static QMTabMenu _tabMenu;

        private static List<QMSingleButton> _buttons = new List<QMSingleButton>();
        private static List<QMNestedButton> _nestedButtons= new List<QMNestedButton>();

        public override void OnInitializeMelon()
        {
            _vmr = new RemoteApiWrapper(PathHelper.GetDllPath());
            if (_vmr.RunVoicemeeter(2) == -1)
                if (_vmr.RunVoicemeeter(3) == -1)
                {
                    MelonLogger.Error("Voicemeeter not found \n Please install Voicemeeter Banana or Potato");
                        return;
                }

            Task.Delay(6000).Wait();
            _vmr.Login();
            _vmr.SetParameter("Recorder.mode.PlayOnLoad", 1);
            MelonCoroutines.Start(WaitForUI());
            base.OnInitializeMelon();
        }

        public override void OnApplicationQuit()
        {
            _vmr.Logout();
            base.OnApplicationQuit();
        }

        public static void PlaySound(string fileName)
        {
            _vmr.SetParameter("Recorder.load", $"{Path}{fileName}");
        }

        private static IEnumerator WaitForUI()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null) yield return null;
            while (APIUtils.QuickMenuInstance.transform.Find("CanvasGroup/Container/Window/QMParent/BackgroundLayer01") == null) yield return null;
            
            Sprite icon = APIUtils.GetQMMenuTemplate().transform.parent.Find("Menu_AudioSettings/Panel_QM_ScrollRect/Viewport/VerticalLayoutGroup/MicrophoneSettings/MicrophoneSettings/Sliders/MicSensitivity&Indicator/Cell_UI_MicActiveIndicator/Icon_On").gameObject.GetComponent<Image>().sprite;
            _tabMenu = new QMTabMenu("Soundboard", "Soundboard", icon);

            QMSingleButton playButton = new QMSingleButton(_tabMenu, 1f, 0f, "Play", delegate { _vmr.SetParameter("recorder.play", 1); }, "Play the last played/recorded sound");
            QMSingleButton stopButton = new QMSingleButton(_tabMenu, 2f, 0f, "Stop", delegate { _vmr.SetParameter("recorder.stop", 1); }, "Stop sound");
            QMToggleButton recordButton = new QMToggleButton(_tabMenu, 3f, 0f, "Record audio", delegate { _vmr.SetParameter("recorder.record", 1); }, delegate { _vmr.SetParameter("recorder.record", 0); _vmr.SetParameter("recorder.stop", 1); }, "Record audio");
            
            QMNestedButton miscButton = new QMNestedButton(_tabMenu, 4f, 0f, "Misc", "Miscellaneous", "Miscellaneous");
            QMSingleButton reloadButton = new QMSingleButton (miscButton, 1f, 0f, $"Reload sounds", delegate { ReloadSounds(); }, "Reload the list of sounds");
            QMToggleButton loopButton = new QMToggleButton(miscButton, 2f, 0f, "Loop", delegate { _vmr.SetParameter("Recorder.mode.Loop", 1); }, delegate { _vmr.SetParameter("Recorder.mode.Loop", 0); }, "Toggle loop");
            QMSingleButton openSoundsFolderButton = new QMSingleButton(miscButton, 3f, 0f, "Open sounds folder", delegate { System.Diagnostics.Process.Start(Path); }, "Open the sounds folder");
            
            miscButton.GetMenuObject().transform.Find("ScrollRect/Viewport/GridLayout")
                .gameObject.GetComponent<GridLayoutGroup>().padding.top = 35;
            RectTransform rectTransform = miscButton.GetMenuObject().transform.Find("ScrollRect/Viewport/GridLayout").GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(1.6f, 1);

            float v = 0;
            _vmr.GetParameter("Recorder.mode.Loop", out v);
            if(v == 1)
                loopButton.SetToggleState(true);
            
            LoadSounds();
        }

        private static void ReloadSounds()
        {
            foreach (QMSingleButton button in _buttons)
            {
                button.DestroyMe();
            }

            _buttons.Clear();

            _files = Directory.GetFiles(Path,  ".").Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3")).ToArray();

            LoadSounds();
        }

        private static void LoadSounds()
        {
            int x = 1;
            int y = 1;
            _files = Directory.GetFiles(Path,  ".").Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3")).ToArray();
            foreach (string file in _files)
            {
                QMSingleButton button = new QMSingleButton(_tabMenu, x, y, System.IO.Path.GetFileNameWithoutExtension(file),
                                 delegate { PlaySound(System.IO.Path.GetFileName(file)); }, "Play the sound"); _buttons.Add(button);
                _buttons.Add(button);
                x++;

                if (x > 4)
                {
                    y++;
                    x = 1;
                }
            }

            _tabMenu.GetMenuObject().transform.Find("ScrollRect/Viewport/GridLayout")
                .gameObject.GetComponent<GridLayoutGroup>().padding.top = 35;
            RectTransform rectTransform = _tabMenu.GetMenuObject().transform.Find("ScrollRect/Viewport/GridLayout")
                .gameObject.GetComponent<RectTransform>();
            rectTransform.transform.parent.gameObject.GetComponent<RectMask2DEx>().enabled = true;
            rectTransform.pivot = new Vector2(0.6f, 1);
        }
    }
}