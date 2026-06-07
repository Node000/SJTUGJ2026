using Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HomeAudioSettingsView : MonoBehaviour
    {
        public Slider bgmVolumeSlider;
        public Slider sfxVolumeSlider;
        public BgmEnum startupBgm = BgmEnum.Phase1;

        private void Awake()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager == null)
                return;

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(manager.bgmVolume);
                bgmVolumeSlider.onValueChanged.AddListener(manager.SetBgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(manager.sfxVolume);
                sfxVolumeSlider.onValueChanged.AddListener(manager.SetSfxVolume);
            }

            AudioManager.PlayBgm(startupBgm);
        }

        private void OnDestroy()
        {
            AudioManager manager = AudioManager.Instance;
            if (manager == null)
                return;

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(manager.SetBgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(manager.SetSfxVolume);
            }
        }
    }
}
