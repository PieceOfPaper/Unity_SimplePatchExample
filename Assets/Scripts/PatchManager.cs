using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatchManager : MonoBehaviour
{
    #region Data

    [System.Serializable]
    public class PatchDataList
    {
        public List<PatchData> dataList = new List<PatchData>();
    }

    [System.Serializable]
    public class PatchData
    {
        public string fileName;
        public long fileSize;
        public int version;
        public string hash;

        public PatchData() { }
        public PatchData(PatchData data) { UpdateData(data); }

        public void UpdateData(PatchData data)
        {
            this.fileName = data.fileName;
            this.fileSize = data.fileSize;
            this.version = data.version;
            this.hash = data.hash;
        }
    }

    public enum PatchStep
    {
        None = 0,

        DownloadPatchList,
        DownloadPatchFiles,
    }

    #endregion

    #region Consts

    const string PATCH_LIST_FILENAME = "PatchList.json";
    const string PATCHED_LIST_FILENAME = "PatchedList.json";
    const string PATCH_BASE_URI = "https://raw.githubusercontent.com/PieceOfPaper/Unity_SimplePatchExample/main/PatchFiles";
    const float SAVE_PATCH_LIST_TIME = 1.0f;
    const string SAVE_PATCH_PATH = "PatchFiles";

    #endregion

    #region Menu Items
#if UNITY_EDITOR

    [UnityEditor.MenuItem("Patch Manager/Build Patch Files")]
    static void BuildPatchFiles()
    {
        var rootPath = Application.dataPath.Replace("/Assets", "");

        //BUILD!!!!
        var patchFolderPath = $"PatchFiles/{UnityEditor.EditorUserBuildSettings.activeBuildTarget}";
        if (System.IO.Directory.Exists(System.IO.Path.Combine(rootPath, patchFolderPath)) == false)
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(rootPath, patchFolderPath));
        UnityEditor.BuildPipeline.BuildAssetBundles(patchFolderPath, UnityEditor.BuildAssetBundleOptions.None, UnityEditor.EditorUserBuildSettings.activeBuildTarget);

        //패치리스트 갱신
        var patchDataListPath = System.IO.Path.Combine(rootPath, patchFolderPath, PATCH_LIST_FILENAME);
        string jsonText = System.IO.File.Exists(patchDataListPath) ? System.IO.File.ReadAllText(patchDataListPath) : null;
        var patchDataList = string.IsNullOrEmpty(jsonText) ? null : JsonUtility.FromJson<PatchDataList>(jsonText);
        if (patchDataList == null) patchDataList = new PatchDataList();

        List<string> pathedFileNameList = new List<string>();
        var assetBundleNames = UnityEditor.AssetDatabase.GetAllAssetBundleNames();
        foreach (var assetBundleName in assetBundleNames)
        {
            var filePath = System.IO.Path.Combine(rootPath, patchFolderPath, assetBundleName);
            if (System.IO.File.Exists(filePath) == false) continue;

            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo == null) continue;

            var fileSize = fileInfo.Length;
            var fileHash = System.Security.Cryptography.MD5.Create().ComputeHash(fileInfo.OpenRead());
            var fileHashStr = System.BitConverter.ToString(fileHash).Replace("-", "").ToLowerInvariant();

            var defaultData = patchDataList.dataList.Find(m => m.fileName == assetBundleName);
            if (defaultData == null)
            {
                var newData = new PatchData();
                newData.fileName = assetBundleName;
                newData.version = 1;
                newData.fileSize = fileSize;
                newData.hash = fileHashStr;
                patchDataList.dataList.Add(newData);
            }
            else
            {
                //hash로 같은 파일인지 체크.
                if (defaultData.hash != fileHashStr)
                {
                    defaultData.version++;
                    defaultData.fileSize = fileSize;
                    defaultData.hash = fileHashStr;
                }
            }
            pathedFileNameList.Add(assetBundleName);
        }

        //삭제된 패치파일의 경우 삭제시켜준다.
        for (int i = 0; i < patchDataList.dataList.Count; i++)
        {
            if (pathedFileNameList.Contains(patchDataList.dataList[i].fileName) == false)
            {
                patchDataList.dataList.RemoveAt(i);
                i--;
            }
        }

        //패치 리스트 저장
        System.IO.File.WriteAllText(patchDataListPath, JsonUtility.ToJson(patchDataList, true));
    }


    [UnityEditor.MenuItem("Patch Manager/Open Persistent Folder")]
    static void OpenPersistentFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

#endif
    #endregion


    static PatchManager m_Instance;
    public static PatchManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                var newObj = new GameObject("PatchManager");
                m_Instance = newObj.AddComponent<PatchManager>();
                m_Instance.Initialize();
            }
            return m_Instance;
        }
    }



    private string m_DataPath;
    private string m_PersistentDataPath;
    private string m_Platform;
    private PatchDataList m_PatchList;
    private PatchDataList m_PatchedList;


    private PatchStep m_CurrentPatchStep = PatchStep.None;
    public PatchStep CurrentPatchStep => m_CurrentPatchStep;

    private float m_DownloadProgress = 0.0f;
    public float DownloadProgress => m_DownloadProgress;

    private float m_FullProgress = 0.0f;
    public float FullProgress => m_FullProgress;

    private int m_DownloadCount = 0;
    public int DownloadCount => m_DownloadCount;

    private int m_DownloadCountMax = 0;
    public int DownloadCountMax => m_DownloadCountMax;



    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Initialize()
    {
        m_DataPath = Application.dataPath;
        m_PersistentDataPath = Application.persistentDataPath;

#if UNITY_EDITOR
        m_Platform = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#else
        m_Platform = Application.platform.ToString();
#endif
    }

    public IEnumerator DownloadPatchListRoutine()
    {
        m_CurrentPatchStep = PatchStep.DownloadPatchList;
        m_DownloadProgress = 0.0f;
        m_FullProgress = 0.0f;
        m_DownloadCount = 0;
        m_DownloadCountMax = 1;

        m_PatchList = null;

        var uri = $"{PATCH_BASE_URI}/{m_Platform}/{PATCH_LIST_FILENAME}";
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(uri))
        {
            request.SendWebRequest();
            while (request.isDone == false)
            {
                m_DownloadProgress = request.downloadProgress;
                m_FullProgress = m_DownloadProgress;
                yield return null;
            }

            if (string.IsNullOrEmpty(request.error) == false)
            {
                Debug.LogError($"[PatchManager] DownloadPatchFile Error - {request.error}");
            }
            else
            {
                m_DownloadCount = m_DownloadCountMax;
                m_PatchList = JsonUtility.FromJson<PatchDataList>(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator DownloadPatchFilesRoutine()
    {
        var pathedListPath = System.IO.Path.Combine(m_PersistentDataPath, SAVE_PATCH_PATH, PATCHED_LIST_FILENAME);
        string jsonText = System.IO.File.Exists(pathedListPath) ? System.IO.File.ReadAllText(pathedListPath) : null;
        m_PatchedList = string.IsNullOrEmpty(jsonText) ? null : JsonUtility.FromJson<PatchDataList>(jsonText);
        if (m_PatchedList == null) m_PatchedList = new PatchDataList();

        List<PatchData> willPatchDataList = new List<PatchData>();
        if (m_PatchList != null)
        {
            foreach (var patchData in m_PatchList.dataList)
            {
                var pathedData = m_PatchedList.dataList.Find(m => m.fileName == patchData.fileName);
                if (pathedData == null || pathedData.version != patchData.version)
                {
                    willPatchDataList.Add(patchData);
                }
            }
        }

        m_CurrentPatchStep = PatchStep.DownloadPatchFiles;
        m_DownloadProgress = 0.0f;
        m_FullProgress = 0.0f;
        m_DownloadCount = 0;
        m_DownloadCountMax = willPatchDataList.Count;

        var saveStartTimeTick = System.DateTime.Now.Ticks;
        for (int i = 0; i < willPatchDataList.Count; i++)
        {
            var patchData = willPatchDataList[i];

            var uri = $"{PATCH_BASE_URI}/{m_Platform}/{patchData.fileName}";
            var savePath = System.IO.Path.Combine(m_PersistentDataPath, SAVE_PATCH_PATH, patchData.fileName);
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(uri))
            {
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(savePath);
                request.SendWebRequest();
                while (request.isDone == false)
                {
                    m_DownloadProgress = request.downloadProgress;
                    m_FullProgress = ((float)m_DownloadCount / m_DownloadCountMax) + (m_DownloadProgress / m_DownloadCountMax);
                    yield return null;
                }

                if (string.IsNullOrEmpty(request.error) == false)
                {
                    Debug.LogError($"[PatchManager] DownloadPatchFiles Error - {request.error}");
                }
                else
                {
                    var pathedData = m_PatchedList.dataList.Find(m => m.fileName == patchData.fileName);
                    if (pathedData == null)
                    {
                        pathedData = new PatchData(patchData);
                        m_PatchedList.dataList.Add(pathedData);
                    }
                    else
                    {
                        pathedData.UpdateData(pathedData);
                    }
                }
            }

            //파일 용량이 작은 파일을 연속해서 받는경우, 패치리스트 파일을 갱신하는데 더 많은 비용이 소모되므로 일정 주기별로 갱신하도록 한다.
            if ((saveStartTimeTick - System.DateTime.Now.Ticks) > SAVE_PATCH_LIST_TIME * System.TimeSpan.TicksPerSecond)
            {
                System.IO.File.WriteAllText(pathedListPath, JsonUtility.ToJson(m_PatchedList, false));
                saveStartTimeTick = System.DateTime.Now.Ticks;
            }

            m_DownloadCount = i + 1;
            m_DownloadProgress = 1.0f;
            m_FullProgress = ((float)m_DownloadCount / m_DownloadCountMax);
        }

        m_DownloadProgress = 1.0f;
        m_FullProgress = 1.0f;
        m_DownloadCount = m_DownloadCountMax;

        //최종 저장
        System.IO.File.WriteAllText(pathedListPath, JsonUtility.ToJson(m_PatchedList, false));
    }

    public System.Threading.Thread DownloadPatchFilesThread()
    {
        var thread = new System.Threading.Thread(() =>
        {
            var pathedListPath = System.IO.Path.Combine(m_PersistentDataPath, SAVE_PATCH_PATH, PATCHED_LIST_FILENAME);
            string jsonText = System.IO.File.Exists(pathedListPath) ? System.IO.File.ReadAllText(pathedListPath) : null;
            m_PatchedList = string.IsNullOrEmpty(jsonText) ? null : JsonUtility.FromJson<PatchDataList>(jsonText);
            if (m_PatchedList == null) m_PatchedList = new PatchDataList();

            List<PatchData> willPatchDataList = new List<PatchData>();
            if (m_PatchList != null)
            {
                foreach (var patchData in m_PatchList.dataList)
                {
                    var pathedData = m_PatchedList.dataList.Find(m => m.fileName == patchData.fileName);
                    if (pathedData == null || pathedData.version != patchData.version)
                    {
                        willPatchDataList.Add(patchData);
                    }
                }
            }

            m_CurrentPatchStep = PatchStep.DownloadPatchFiles;
            m_DownloadProgress = 0.0f;
            m_FullProgress = 0.0f;
            m_DownloadCount = 0;
            m_DownloadCountMax = willPatchDataList.Count;

            var saveStartTimeTick = System.DateTime.Now.Ticks;
            for (int i = 0; i < willPatchDataList.Count; i++)
            {
                var patchData = willPatchDataList[i];

                var uri = $"{PATCH_BASE_URI}/{m_Platform}/{patchData.fileName}";
                var savePath = System.IO.Path.Combine(m_PersistentDataPath, SAVE_PATCH_PATH, patchData.fileName);
                CreateDirectoryByFilePath(System.IO.Path.Combine(m_PersistentDataPath, SAVE_PATCH_PATH), patchData.fileName);

                if (System.IO.File.Exists(savePath))
                    System.IO.File.Delete(savePath);

                using (var webClient = new System.Net.WebClient())
                {
                    var ur = new System.Uri(uri);
                    webClient.DownloadProgressChanged += (object sender, System.Net.DownloadProgressChangedEventArgs e) => {
                        m_DownloadProgress = e.ProgressPercentage;
                        m_FullProgress = ((float)m_DownloadCount / m_DownloadCountMax) + (m_DownloadProgress / m_DownloadCountMax);
                    };

                    bool isCompleted = false;
                    webClient.DownloadFileCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) => {
                        isCompleted = true;
                    };
                    
                    webClient.DownloadFileAsync(ur, savePath);
                    while(isCompleted == false) System.Threading.Thread.Sleep(100);

                    var fileExist = System.IO.File.Exists(savePath);
                    if (fileExist == false)
                    {
                        //TODO - 로그를 따로 뺀다.
                        // Debug.LogError($"[PatchManager] DownloadPatchFiles Error - {request.error}");
                    }
                    else
                    {
                        var pathedData = m_PatchedList.dataList.Find(m => m.fileName == patchData.fileName);
                        if (pathedData == null)
                        {
                            pathedData = new PatchData(patchData);
                            m_PatchedList.dataList.Add(pathedData);
                        }
                        else
                        {
                            pathedData.UpdateData(pathedData);
                        }
                    }
                }

                //파일 용량이 작은 파일을 연속해서 받는경우, 패치리스트 파일을 갱신하는데 더 많은 비용이 소모되므로 일정 주기별로 갱신하도록 한다.
                if ((saveStartTimeTick - System.DateTime.Now.Ticks) > SAVE_PATCH_LIST_TIME * System.TimeSpan.TicksPerSecond)
                {
                    System.IO.File.WriteAllText(pathedListPath, JsonUtility.ToJson(m_PatchedList, false));
                    saveStartTimeTick = System.DateTime.Now.Ticks;
                }

                m_DownloadCount = i + 1;
                m_DownloadProgress = 1.0f;
                m_FullProgress = ((float)m_DownloadCount / m_DownloadCountMax);
            }

            m_DownloadProgress = 1.0f;
            m_FullProgress = 1.0f;
            m_DownloadCount = m_DownloadCountMax;

            //최종 저장
            System.IO.File.WriteAllText(pathedListPath, JsonUtility.ToJson(m_PatchedList, false));

        });
        return thread;
    }

    private void CreateDirectoryByFilePath(string basePath, string filePath)
    {
        var currentPath = basePath;
        var splited = filePath.Replace('\\', '/').Split('/');
        for (int i = 0; i < splited.Length - 1; i ++)
        {
            currentPath += '/' + splited[i];
            if (System.IO.Directory.Exists(currentPath) == false)
                System.IO.Directory.CreateDirectory(currentPath);
        }
    }
}