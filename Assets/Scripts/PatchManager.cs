using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatchManager
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
        public int version;
        public string hash;
    }

    #endregion

    #region Consts

    const string PATCH_LIST_FILENAME = "PatchList.json";
    const string PATCHED_LIST_FILENAME = "PatchedList.json";

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

            var fileHash = System.Security.Cryptography.MD5.Create().ComputeHash(fileInfo.OpenRead());
            var fileHashStr = System.BitConverter.ToString(fileHash).Replace("-", "").ToLowerInvariant();

            var defaultData = patchDataList.dataList.Find(m => m.fileName == assetBundleName);
            if (defaultData == null)
            {
                var newData = new PatchData();
                newData.fileName = assetBundleName;
                newData.version = 1;
                newData.hash = fileHashStr;
                patchDataList.dataList.Add(newData);
            }
            else
            {
                //hash로 같은 파일인지 체크.
                if (defaultData.hash != fileHashStr)
                {
                    defaultData.version++;
                    defaultData.hash = fileHashStr;
                }
            }
            pathedFileNameList.Add(assetBundleName);
        }

        //삭제된 패치파일의 경우 삭제시켜준다.
        for (int i = 0; i < patchDataList.dataList.Count; i ++)
        {
            if (pathedFileNameList.Contains(patchDataList.dataList[i].fileName) == false)
            {
                patchDataList.dataList.RemoveAt(i);
                i --;
            }
        }

        //패치 리스트 저장
        System.IO.File.WriteAllText(patchDataListPath, JsonUtility.ToJson(patchDataList, true));
    }

    #endif
    #endregion


    static PatchManager m_Instance;
    public PatchManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new PatchManager();
                m_Instance.Initialize();
            }
            return m_Instance;
        }
    }



    string m_DataPath;
    string m_PersistentDataPath;
    PatchDataList m_PatchedList;


    private void Initialize()
    {
        m_DataPath = Application.dataPath;
        m_PersistentDataPath = Application.persistentDataPath;
    }

    public void LoadPatchedList()
    {
        string jsonText = System.IO.File.ReadAllText(System.IO.Path.Combine(m_PersistentDataPath, PATCHED_LIST_FILENAME));
        m_PatchedList = string.IsNullOrEmpty(jsonText) ? null : JsonUtility.FromJson<PatchDataList>(jsonText);
    }
}
