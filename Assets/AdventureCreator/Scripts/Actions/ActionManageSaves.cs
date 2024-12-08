/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"ActionManageSaves.cs"
 * 
 *	This Action renames and deletes save game files
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionManageSaves : Action
	{
		
		public ManageSaveType manageSaveType = ManageSaveType.DeleteSave;
		public SelectSaveType selectSaveType = SelectSaveType.SetSlotIndex;

		public int saveIndex = 0;
		public int saveIndexParameterID = -1;

		public int varID;
		public int slotVarID;
		
		public string menuName = "";
		public string elementName = "";
		
		
		public override ActionCategory Category { get { return ActionCategory.Save; }}
		public override string Title { get { return "Manage saves"; }}
		public override string Description { get { return "Renames and deletes save game files."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			saveIndex = AssignInteger (parameters, saveIndexParameterID, saveIndex);
		}
		
		
		public override float Run ()
		{
			string newSaveLabel = string.Empty;
			if (manageSaveType == ManageSaveType.RenameSave)
			{
				GVar gVar = GlobalVariables.GetVariable (varID);
				if (gVar != null)
				{
					newSaveLabel = gVar.TextValue;
				}
				else
				{
					LogWarning ("Could not " + manageSaveType.ToString () + " - no variable found.");
					return 0f;
				}
			}

			int i = Mathf.Max (0, saveIndex);
			
			if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
			{
				GVar gVar = GlobalVariables.GetVariable (slotVarID);
				if (gVar != null)
				{
					i = gVar.IntegerValue;
				}
				else
				{
					LogWarning ("Could not rename save - no variable found.");
					return 0f;
				}
			}
			else if (selectSaveType == SelectSaveType.Autosave)
			{
				if (manageSaveType == ManageSaveType.DeleteSave)
				{
					SaveSystem.DeleteSave (0);
					return 0f;
				}
				else if (manageSaveType == ManageSaveType.RenameSave)
				{
					LogWarning ("The Autosave file cannot be renamed.");
					return 0f;
				}
			}

			if (selectSaveType != SelectSaveType.Autosave && selectSaveType != SelectSaveType.SetSaveID)
			{
				if (!string.IsNullOrEmpty (menuName) && !string.IsNullOrEmpty (elementName))
				{
					MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
					if (menuElement != null && menuElement is MenuSavesList)
					{
						MenuSavesList menuSavesList = (MenuSavesList) menuElement;
						i += menuSavesList.GetOffset ();
					}
					else
					{
						LogWarning ("Cannot find SavesList element '" + elementName + "' in Menu '" + menuName + "'.");
					}
				}
				else
				{
					LogWarning ("No SavesList element referenced when trying to find save slot " + i.ToString ());
				}
			}
			
			if (manageSaveType == ManageSaveType.DeleteSave)
			{
				if (selectSaveType == SelectSaveType.SetSaveID)
				{
					SaveSystem.DeleteSave (i);
				}
				else
				{
					KickStarter.saveSystem.DeleteSave (i, -1, false);
				}
			}
			else if (manageSaveType == ManageSaveType.RenameSave)
			{
				if (selectSaveType == SelectSaveType.SetSaveID)
				{
					KickStarter.saveSystem.RenameSaveByID (newSaveLabel, i);
				}
				else
				{
					KickStarter.saveSystem.RenameSave (newSaveLabel, i);
				}
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			manageSaveType = (ManageSaveType) EditorGUILayout.EnumPopup ("Method:", manageSaveType);
			
			if (manageSaveType == ManageSaveType.RenameSave)
			{
				varID = AdvGame.GlobalVariableGUI ("Label as String variable:", varID, VariableType.String);
			}

			string _action = "delete";
			if (manageSaveType == ManageSaveType.RenameSave)
			{
				_action = "rename";
			}
			
			selectSaveType = (SelectSaveType) EditorGUILayout.EnumPopup ("Save to " + _action + ":", selectSaveType);
			if (selectSaveType == SelectSaveType.SetSlotIndex)
			{
				saveIndexParameterID = Action.ChooseParameterGUI ("Slot index to " + _action + ":", parameters, saveIndexParameterID, ParameterType.Integer);
				if (saveIndexParameterID == -1)
				{
					saveIndex = EditorGUILayout.IntField ("Slot index to " + _action + ":", saveIndex);
				}
			}
			else if (selectSaveType == SelectSaveType.SlotIndexFromVariable)
			{
				slotVarID = AdvGame.GlobalVariableGUI ("Integer variable:", slotVarID, VariableType.Integer);
			}
			else if (selectSaveType == SelectSaveType.Autosave && manageSaveType == ManageSaveType.RenameSave)
			{
				EditorGUILayout.HelpBox ("The AutoSave cannot be renamed.", MessageType.Warning);
				return;
			}
			else if (selectSaveType == SelectSaveType.SetSaveID)
			{
				saveIndexParameterID = Action.ChooseParameterGUI ("Save ID to " + _action + ":", parameters, saveIndexParameterID, ParameterType.Integer);
				if (saveIndexParameterID == -1)
				{
					saveIndex = EditorGUILayout.IntField ("Save ID to " + _action + ":", saveIndex);
				}
			}

			if (selectSaveType != SelectSaveType.Autosave && selectSaveType != SelectSaveType.SetSaveID)
			{
				EditorGUILayout.Space ();
				menuName = EditorGUILayout.TextField ("Menu with SavesList:", menuName);
				elementName = EditorGUILayout.TextField ("SavesList element:", elementName);
			}
		}
		
		
		public override string SetLabel ()
		{
			return manageSaveType.ToString ();
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Save: Manage saves' Action, set to delete a save file</summary>
		 * <param name = "menuName">The name of the menu containing the SavesList element</param>
		 * <param name = "savesListElementName">The name of the SavesList element</param>
		 * <param name = "slotIndex">The slot index to delete. If negative, the Autosave will be deleted</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageSaves CreateNew_DeleteSave (string menuName, string savesListElementName, int slotIndex = -1)
		{
			ActionManageSaves newAction = CreateNew<ActionManageSaves> ();
			newAction.manageSaveType = ManageSaveType.DeleteSave;
			newAction.selectSaveType = (slotIndex < 0) ? SelectSaveType.Autosave : SelectSaveType.SetSlotIndex;
			newAction.saveIndex = slotIndex;
			newAction.menuName = menuName;
			newAction.elementName = savesListElementName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Manage saves' Action, set to rename a save file</summary>
		 * <param name = "menuName">The name of the menu containing the SavesList element</param>
		 * <param name = "savesListElementName">The name of the SavesList element</param>
		 * <param name = "selectSavlabelGlobalStringVariableID">The ID number of a Global String variable whose value will be used to rename the file with</param>
		 * <param name = "slotIndex">The slot index to rename</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageSaves CreateNew_RenameSave (string menuName, string savesListElementName, int labelGlobalStringVariableID, int slotIndex)
		{
			ActionManageSaves newAction = CreateNew<ActionManageSaves> ();
			newAction.manageSaveType = ManageSaveType.RenameSave;
			newAction.selectSaveType = SelectSaveType.SetSlotIndex;
			newAction.slotVarID = labelGlobalStringVariableID;
			newAction.menuName = menuName;
			newAction.elementName = savesListElementName;
			return newAction;
		}
		
	}
	
}