﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using PlayFab.Editor.EditorModels;

namespace PlayFab.Editor
{
    [InitializeOnLoad]
    public class PlayFabEditorDataService : UnityEditor.Editor {
        public static PlayFab_DeveloperAccountDetails accountDetails;
        public static PlayFab_DeveloperEnvironmentDetails envDetails;
        public static PlayFab_EditorSettings editorSettings;

        public static EditorModels.Title activeTitle 
       { 
            get {
                if(accountDetails != null && accountDetails.studios.Count > 0)
                {
                    if(envDetails != null && !string.IsNullOrEmpty(envDetails.selectedStudio) && !string.IsNullOrEmpty(envDetails.selectedTitleId))
                    {
                        try
                        {
                            return accountDetails.studios.Find((EditorModels.Studio item) => { return item.Name == envDetails.selectedStudio; }).Titles.First((EditorModels.Title title) => { return title.Id == envDetails.selectedTitleId; });
                        }
                        catch(Exception ex)
                        {
                            PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
                        }
                    }
                    return accountDetails.studios[0].Titles[0]; 
                }
                return null;
            }
       }

        public static void SaveAccountDetails()
        {
            try
            {
                var serialized = Json.JsonWrapper.SerializeObject(accountDetails);
                EditorPrefs.SetString("PlayFab_DeveloperAccountDetails", serialized);
            }
            catch(Exception ex)
            {
                PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
            }
        }

        public static void SaveEnvDetails()
        {
            try
            {
                var serialized = Json.JsonWrapper.SerializeObject(envDetails);
                EditorPrefs.SetString("PlayFab_DeveloperEnvironmentDetails", serialized);

                //update scriptable object
                UpdateScriptableObject();
            }
            catch(Exception ex)
            {
                PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
            }
        }

        public static void SaveEditorSettings()
        {
            try
            {
                var serialized = Json.JsonWrapper.SerializeObject(editorSettings);
                EditorPrefs.SetString("PlayFab_EditorSettings", serialized);
            }
            catch(Exception ex)
            {
                PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
            }
        }

        public static void LoadAccountDetails()
        {
            if(EditorPrefs.HasKey("PlayFab_DeveloperAccountDetails"))
            {
                var serialized = EditorPrefs.GetString("PlayFab_DeveloperAccountDetails");
                try
                {
                    accountDetails = Json.JsonWrapper.DeserializeObject<PlayFab_DeveloperAccountDetails>(serialized);
                    return;

                }
                catch(Exception ex)
                {
                    PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
                }
            }
            accountDetails = new PlayFab_DeveloperAccountDetails();
        }

        public static void LoadEnvDetails()
        {
            if(EditorPrefs.HasKey("PlayFab_DeveloperEnvironmentDetails"))
            {
                var serialized = EditorPrefs.GetString("PlayFab_DeveloperEnvironmentDetails");
                try
                {
                    envDetails = Json.JsonWrapper.DeserializeObject<PlayFab_DeveloperEnvironmentDetails>(serialized);

                    return;

                }
                catch(Exception ex)
                {
                    PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
                }
            }
            envDetails = new PlayFab_DeveloperEnvironmentDetails();


        }

        public static void LoadEditorSettings()
        {
            if(EditorPrefs.HasKey("PlayFab_EditorSettings"))
            {
                var serialized = EditorPrefs.GetString("PlayFab_EditorSettings");
                try
                {
                    editorSettings = Json.JsonWrapper.DeserializeObject<PlayFab_EditorSettings>(serialized);
                    LoadFromScriptableObject();
                    return;
                }
                catch(Exception ex)
                {
                    PlayFabEditor.RaiseStateUpdate(PlayFabEditor.EdExStates.OnError, ex.Message);
                }
            }
            editorSettings = new PlayFab_EditorSettings();
        }

        public static void SaveAllData()
        {
            SaveAccountDetails();
            SaveEnvDetails();
            SaveEditorSettings();
        }

        public static void LoadAllData()
        {
            LoadAccountDetails();
            LoadEnvDetails();
            LoadEditorSettings();

            //TODO make sure this should be called here...
            LoadFromScriptableObject();
        }

        public static void LoadFromScriptableObject()
        {
            if(envDetails == null)
                return;
               
                var playfabSettingsType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.Name == "PlayFabSettings"
                    select type);

                if (playfabSettingsType.ToList().Count > 0)
                {
                    var type = playfabSettingsType.ToList().FirstOrDefault();
                    var props = type.GetProperties();

                    envDetails.selectedTitleId = (string) props.ToList().Find(p => p.Name == "TitleId").GetValue(null, null) ?? envDetails.selectedTitleId;
                    envDetails.webRequestType = (PlayFabEditorSettings.WebRequestType)props.ToList().Find(p => p.Name == "RequestType").GetValue(null, null);
                    envDetails.timeOut = (int) props.ToList().Find(p => p.Name == "RequestTimeout").GetValue(null, null);
                    envDetails.keepAlive = (bool) props.ToList().Find(p => p.Name == "RequestKeepAlive").GetValue(null, null);
                    envDetails.compressApiData = (bool)props.ToList().Find(p => p.Name == "CompressApiData").GetValue(null, null);



#if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                    envDetails.developerSecretKey = (string) props.ToList().Find(p => p.Name == "DeveloperSecretKey").GetValue(null, null) ?? envDetails.developerSecretKey;
#else
                    envDetails.developerSecretKey = string.Empty;
#endif
                }
        }
      
        public static void UpdateScriptableObject()
        {
                //TODO move this logic to the data service
                var playfabSettingsType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.Name == "PlayFabSettings"
                    select type);
                if (playfabSettingsType.ToList().Count > 0)
                {
                    var type = playfabSettingsType.ToList().FirstOrDefault();
                   // var fields = type.GetFields();
                    var props = type.GetProperties();

                    foreach (var property in props)
                    {
                        //Debug.Log(property.Name);
                        if (property.Name.ToLower() == "titleid")
                        {
                            property.SetValue(null, envDetails.selectedTitleId, null);
                        }
                        if (property.Name.ToLower() == "requesttype")
                        {
                            property.SetValue(null, (int)envDetails.webRequestType, null);
                        }
                        if (property.Name.ToLower() == "timeout")
                        {
                            property.SetValue(null, envDetails.timeOut, null);
                        }
                        if (property.Name.ToLower() == "requestkeepalive")
                        {
                            property.SetValue(null, envDetails.keepAlive, null);
                        }
                        if (property.Name.ToLower() == "compressapidata")
                        {
                            property.SetValue(null, envDetails.compressApiData, null);
                        }

                        //this is a fix for the S.O. getting blanked repeatedly.
                        if (property.Name.ToLower() == "productionenvironmenturl")
                        {
                            property.SetValue(null, PlayFabEditorHelper.TITLE_ENDPOINT, null);
                        }


#if ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFABSERVER_API
                        if (property.Name.ToLower() == "developersecretkey")
                        {
                            property.SetValue(null, envDetails.developerSecretKey, null);
                        }
#endif
                    }

                    AssetDatabase.SaveAssets();
                 }
        }


        //CTOR
        static PlayFabEditorDataService()
        {
            LoadAllData();
        }

    }
}