using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScene_PatchPage : MonoBehaviour
{
    [SerializeField] int m_Type = 1;
    [SerializeField] TMPro.TextMeshProUGUI m_TextPatchStep;
    [SerializeField] UnityEngine.UI.Slider m_SliderDownloadProgress;
    [SerializeField] UnityEngine.UI.Slider m_SliderFullProgress;
    [SerializeField] TMPro.TextMeshProUGUI m_TextDownloadCount;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return StartCoroutine(PatchManager.Instance.DownloadPatchListRoutine());

        switch(m_Type)
        {
            case 1:
                {
                    yield return StartCoroutine(PatchManager.Instance.DownloadPatchFilesRoutine());
                }
                break;
            case 2:
                {
                    var thread = PatchManager.Instance.DownloadPatchFilesThread();
                    thread.Start();
                    yield return null;
                    while(PatchManager.Instance.DownloadCount < PatchManager.Instance.DownloadCountMax) yield return null;
                    thread.Abort();
                }
                break;
        }
        Debug.Log("다운로드까지 걸린 시간: " + PatchManager.Instance.DownloadPatchFilesTimeSpan);
    }

    void LateUpdate()
    {
        if (m_TextPatchStep != null) m_TextPatchStep.text = PatchManager.Instance.CurrentPatchStep.ToString();
        if (m_SliderDownloadProgress != null) m_SliderDownloadProgress.value = PatchManager.Instance.DownloadProgress;
        if (m_SliderFullProgress != null) m_SliderFullProgress.value = PatchManager.Instance.FullProgress;
        if (m_TextDownloadCount != null) m_TextDownloadCount.text = $"{PatchManager.Instance.DownloadCount}/{PatchManager.Instance.DownloadCountMax}";
    }
}
