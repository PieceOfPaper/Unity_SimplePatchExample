using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SampleScene_PatchPage : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] int m_Type = 1;

    [Header("Progress")]
    [SerializeField] TMPro.TextMeshProUGUI m_TextPatchStep;
    [SerializeField] UnityEngine.UI.Slider m_SliderDownloadProgress;
    [SerializeField] UnityEngine.UI.Slider m_SliderFullProgress;
    [SerializeField] TMPro.TextMeshProUGUI m_TextDownloadCount;

    [Header("Popup")]
    [SerializeField] GameObject m_ObjPopup;
    [SerializeField] TMPro.TextMeshProUGUI m_TextPopup;

    bool m_IsReadyToPopup;

    private void Awake()
    {
        if (m_ObjPopup != null) m_ObjPopup.SetActive(false);
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return StartCoroutine(PatchManager.Instance.DownloadPatchListRoutine());

        var needPatchDatas = PatchManager.Instance.GetNeedPatchDatas();
        if (needPatchDatas == null || needPatchDatas.Count() == 0)
        {
            if (m_ObjPopup != null) m_ObjPopup.SetActive(true);
            if (m_TextPopup != null) m_TextPopup.text = "No More Download";
            m_IsReadyToPopup = true;
            yield break;
        }
        else
        {
            long fileSize = 0;
            foreach (var data in needPatchDatas)
                fileSize += data.fileSize;

            float unit_KB = 1024f;
            float unit_MB = 1024f * 1024f;
            string fileSizeStr = fileSize > unit_MB ? 
                string.Format("{0:f2}MB", (fileSize / unit_MB)) : 
                string.Format("{0:f2}KB", (fileSize / unit_KB));

            if (m_ObjPopup != null) m_ObjPopup.SetActive(true);
            if (m_TextPopup != null) m_TextPopup.text = $"Download Popup\nFile Count: {needPatchDatas.Count()}\nFile Size: {fileSizeStr}";
            m_IsReadyToPopup = true;
        }
        while(m_IsReadyToPopup == true) yield return null;

        switch(m_Type)
        {
            case 1:
                {
                    yield return StartCoroutine(PatchManager.Instance.DownloadPatchFilesRoutine(needPatchDatas));
                }
                break;
            case 2:
                {
                    var thread = PatchManager.Instance.DownloadPatchFilesThread(needPatchDatas);
                    thread.Start();
                    yield return null;
                    while(PatchManager.Instance.DownloadCount < PatchManager.Instance.DownloadCountMax) yield return null;
                    thread.Abort();
                }
                break;
        }
        Debug.Log("?????????????????? ?????? ??????: " + PatchManager.Instance.DownloadPatchFilesTimeSpan);

        if (m_ObjPopup != null) m_ObjPopup.SetActive(true);
        if (m_TextPopup != null) m_TextPopup.text = "Download Complete";
    }

    void LateUpdate()
    {
        if (m_TextPatchStep != null) m_TextPatchStep.text = PatchManager.Instance.CurrentPatchStep.ToString();
        if (m_SliderDownloadProgress != null) m_SliderDownloadProgress.value = PatchManager.Instance.DownloadProgress;
        if (m_SliderFullProgress != null) m_SliderFullProgress.value = PatchManager.Instance.FullProgress;
        if (m_TextDownloadCount != null) m_TextDownloadCount.text = $"{PatchManager.Instance.DownloadCount}/{PatchManager.Instance.DownloadCountMax}";
    }


    public void OnClick_OK()
    {
        if (m_ObjPopup != null) m_ObjPopup.SetActive(false);
        m_IsReadyToPopup = false;
    }
}
