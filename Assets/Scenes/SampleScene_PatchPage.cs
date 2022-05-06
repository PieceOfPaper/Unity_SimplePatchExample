using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScene_PatchPage : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI m_TextPatchStep;
    [SerializeField] UnityEngine.UI.Slider m_SliderDownloadProgress;
    [SerializeField] UnityEngine.UI.Slider m_SliderFullProgress;
    [SerializeField] TMPro.TextMeshProUGUI m_TextDownloadCount;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return StartCoroutine(PatchManager.Instance.DownloadPatchListRoutine());
        yield return StartCoroutine(PatchManager.Instance.DownloadPatchFilesRoutine());
    }

    void LateUpdate()
    {
        if (m_TextPatchStep != null) m_TextPatchStep.text = PatchManager.Instance.CurrentPatchStep.ToString();
        if (m_SliderDownloadProgress != null) m_SliderDownloadProgress.value = PatchManager.Instance.DownloadProgress;
        if (m_SliderFullProgress != null) m_SliderFullProgress.value = PatchManager.Instance.FullProgress;
        if (m_TextDownloadCount != null) m_TextDownloadCount.text = $"{PatchManager.Instance.DownloadCount}/{PatchManager.Instance.DownloadCountMax}";
    }
}
