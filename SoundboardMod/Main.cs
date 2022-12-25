using System;
using System.Collections;
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
        private static readonly string _path = $"{Directory.GetCurrentDirectory()}\\Sounds\\";
        private static readonly string[] _files = Directory.GetFiles(_path,  ".").Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3")).ToArray();

        public override void OnInitializeMelon()
        {
            _vmr = new RemoteApiWrapper(PathHelper.GetDllPath());
            int type = 2;
            _vmr.GetVoicemeeterType(out type);
            _vmr.RunVoicemeeter(type);
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
            _vmr.SetParameter("Recorder.load", $"{_path}{fileName}");
        }

        private static IEnumerator WaitForUI()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null) yield return null;
            while (APIUtils.QuickMenuInstance.transform.Find("CanvasGroup/Container/Window/QMParent/BackgroundLayer01") == null) yield return null;
            Sprite icon = APIUtils.GetQMMenuTemplate().transform.parent
                .Find(
                    "Menu_AudioSettings/Panel_QM_ScrollRect/Viewport/VerticalLayoutGroup/MicrophoneSettings/Sliders/MicSensitivity&Indicator/Cell_UI_MicActiveIndicator/Icon_On")
                .gameObject.GetComponent<Image>().sprite;
            QMTabMenu tabMenu = new QMTabMenu("Soundboard", "Soundboard", icon);
            bool flag = false;
            QMNestedButton nestedButton = null;
            QMSingleButton stopButton = new QMSingleButton (tabMenu, 1f, 0f, $"Stop Sound", delegate { _vmr.SetParameter("recorder.stop", 1);}, "Stop the current sound");
            QMToggleButton loopButton = new QMToggleButton(tabMenu, 2f, 0f, "Loop sound", delegate { _vmr.SetParameter("Recorder.mode.Loop", 1); }, delegate { _vmr.SetParameter("Recorder.mode.Loop", 0); },  "Loop the current sound", false);
            QMSingleButton playButton = new QMSingleButton(tabMenu, 3f, 0f, "Play Sound", delegate { _vmr.SetParameter("recorder.play", 1); }, "Play the last played/recorded sound");
            QMToggleButton recordButton = new QMToggleButton(tabMenu, 4f, 0f, "Record audio", delegate { _vmr.SetParameter("recorder.record", 1); }, delegate { _vmr.SetParameter("recorder.record", 0); _vmr.SetParameter("recorder.stop", 1); }, "Record audio", false);

            int x = 1;
            int y = 1;
            foreach (string file in _files)
            {
                if (!flag)
                {
                    QMSingleButton button = new QMSingleButton(tabMenu, x, y, Path.GetFileNameWithoutExtension(file),
                        delegate { PlaySound(Path.GetFileName(file)); }, "Play the sound");
                }
                else
                {
                    QMSingleButton button = new QMSingleButton(nestedButton, x, y, Path.GetFileNameWithoutExtension(file), delegate { PlaySound(Path.GetFileName(file)); }, "Play the sound");
                }
                x++;
                if (x == 4 && y == 3 && !flag)
                {
                    if (_files.Length > 12)
                    {
                        nestedButton = new QMNestedButton(tabMenu, x, y, "More", "More sounds", "More sounds");
                        flag = true;
                        x = 1;
                        y = 0;
                    }
                }
                else if(x == 4 && y == 3 && flag)
                {
                    nestedButton = new QMNestedButton(nestedButton, x, y, "More", "More sounds", "More sounds");

                    x = 1;
                    y = 0;
                }

                if (x > 4)
                {
                    y++;
                    x = 1;
                }
            }
        }
    }
}