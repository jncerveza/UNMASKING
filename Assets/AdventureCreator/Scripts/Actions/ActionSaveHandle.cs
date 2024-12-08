/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"ActionSaveHandle.cs"
 * 
 *	This Action saves and loads save game files
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

    [System.Serializable]
    public class ActionSaveHandle : Action
    {
        public SaveHandling saveHandling = SaveHandling.LoadGame;
        public SelectSaveType selectSaveType = SelectSaveType.Autosave;

        public int saveIndex = 0;
        public int saveIndexParameterID = -1;

        public int varID;
        public int slotVarID;

        public string menuName = "";
        public string elementName = "";

        public bool updateLabel = false;
        public bool customLabel = false;

        public bool doSelectiveLoad = false;
        public SelectiveLoad selectiveLoad = new SelectiveLoad();
        protected bool recievedCallback;


        public override ActionCategory Category { get { return ActionCategory.Save; } }
        public override string Title { get { return "Save or load"; } }
        public override string Description { get { return "Saves and loads save-game files"; } }
        public override int NumSockets { get { return (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame) ? 1 : 0; } }

        public override void AssignValues(List<ActionParameter> parameters)
        {
            saveIndex = AssignInteger(parameters, saveIndexParameterID, saveIndex);
        }


        public override float Run()
        {
            if (!isRunning)
            {
                isRunning = true;
                recievedCallback = false;

                PerformSaveOrLoad();
            }

            if (recievedCallback)
            {
                isRunning = false;
                return 0f;
            }

            return defaultPauseTime;
        }

        protected void PerformSaveOrLoad()
        {
            ClearAllEvents();

            if (saveHandling == SaveHandling.ContinueFromLastSave || saveHandling == SaveHandling.LoadGame)
            {
                EventManager.OnFinishLoading += OnFinishLoading;
                EventManager.OnFailLoading += OnFail;
            }
            else if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
            {
                EventManager.OnFinishSaving += OnFinishSaving;
                EventManager.OnFailSaving += OnFail;
            }

            if ((saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.ContinueFromLastSave) && doSelectiveLoad)
            {
                KickStarter.saveSystem.SetSelectiveLoadOptions(selectiveLoad);
            }

            string newSaveLabel = string.Empty;
            if (customLabel && ((updateLabel && saveHandling == SaveHandling.OverwriteExistingSave) || saveHandling == AC.SaveHandling.SaveNewGame))
            {
                if (selectSaveType != SelectSaveType.Autosave)
                {
                    GVar gVar = GlobalVariables.GetVariable(varID);
                    if (gVar != null)
                    {
                        newSaveLabel = gVar.GetValue(Options.GetLanguage());
                    }
                    else
                    {
                        LogWarning("Could not " + saveHandling.ToString() + " - no variable found.");
                        return;
                    }
                }
            }

            int i = saveIndex;

            if (saveHandling == SaveHandling.ContinueFromLastSave)
            {
                SaveSystem.ContinueGame();
                return;
            }

            if (saveHandling == SaveHandling.LoadGame || saveHandling == SaveHandling.OverwriteExistingSave)
            {
                if (selectSaveType == SelectSaveType.Autosave)
                {
                    if (saveHandling == SaveHandling.LoadGame)
                    {
                        SaveSystem.LoadAutoSave();
                        return;
                    }
                    else
                    {
                        if (PlayerMenus.IsSavingLocked(this, true))
                        {
                            OnComplete();
                        }
                        else
                        {
                            SaveSystem.SaveAutoSave();
                        }
                        return;
                    }
                }
                else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
                {
                    GVar gVar = GlobalVariables.GetVariable(slotVarID);
                    if (gVar != null)
                    {
                        i = gVar.IntegerValue;
                    }
                    else
                    {
                        LogWarning("Could not get save slot index - no variable found.");
                        return;
                    }
                }
            }

            if (selectSaveType != SelectSaveType.Autosave && selectSaveType != SelectSaveType.SetSaveID)
            {
                if (!string.IsNullOrEmpty(menuName) && !string.IsNullOrEmpty(elementName))
                {
                    MenuElement menuElement = PlayerMenus.GetElementWithName(menuName, elementName);
                    if (menuElement != null && menuElement is MenuSavesList)
                    {
                        MenuSavesList menuSavesList = (MenuSavesList)menuElement;
                        i += menuSavesList.GetOffset();
                    }
                    else
                    {
                        LogWarning("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
                    }
                }
                else
                {
                    LogWarning("No SavesList element referenced when trying to find slot slot " + i.ToString());
                }
            }

            if (saveHandling == SaveHandling.LoadGame)
            {
                if (selectSaveType == SelectSaveType.SetSaveID)
                {
                    SaveSystem.LoadGame(i);
                }
                else
                {
                    SaveSystem.LoadGame(i, -1, false);
                }
            }
            else if (saveHandling == SaveHandling.OverwriteExistingSave || saveHandling == SaveHandling.SaveNewGame)
            {
                if (PlayerMenus.IsSavingLocked(this, true))
                {
                    OnComplete();
                }
                else
                {
                    if (saveHandling == SaveHandling.OverwriteExistingSave)
                    {
                        if (selectSaveType == SelectSaveType.SetSaveID)
                        {
                            SaveSystem.SaveGame(0, i, true, updateLabel, newSaveLabel);
                        }
                        else
                        {
                            SaveSystem.SaveGame(i, -1, false, updateLabel, newSaveLabel);
                        }
                    }
                    else if (saveHandling == SaveHandling.SaveNewGame)
                    {
                        SaveSystem.SaveNewGame(updateLabel, newSaveLabel);
                    }
                }
            }
        }

   

        protected void OnFinishLoading()
        {
            OnComplete();
        }

        protected void OnFinishSaving(SaveFile saveFile)
        {
            OnComplete();

        }

        protected void OnComplete()
        {
            ClearAllEvents();
            recievedCallback = true;
        }

        protected void OnFail(int saveID)
        {
            OnComplete();
        }

        protected void ClearAllEvents()
        {
            EventManager.OnFinishLoading -= OnFinishLoading;
            EventManager.OnFailLoading -= OnFail;

            EventManager.OnFinishSaving -= OnFinishSaving;
            EventManager.OnFailSaving -= OnFail;
        }

#if UNITY_EDITOR

        // GUI and other methods omitted for brevity...

#endif
    }
}
