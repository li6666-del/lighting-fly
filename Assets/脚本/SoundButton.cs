using UnityEngine;

public class SoundButton : MonoBehaviour
{
    private bool isMute = false;

    void Start()
    {
        if (AudioManager.Instance != null)
            isMute = AudioManager.Instance.IsMuted;
        DoUpdate();
    }

    public void Toggle()
    {
        isMute = !isMute;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleBGM();
        DoUpdate();
    }

    private void DoUpdate()
    {
        if (transform.childCount > 1)
        {
            transform.GetChild(1).gameObject.SetActive(!isMute);
        }
        if (transform.childCount > 2)
        {
            transform.GetChild(2).gameObject.SetActive(isMute);
        }
    }
}
