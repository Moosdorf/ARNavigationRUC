using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptionsDropdown : MonoBehaviour
{

    [SerializeField] private TMP_Dropdown dropdown;
    void Start()
    {
        List<Waypoint> endNodes = MainManager.routeController.GetEndNodes(); // the lines controller have info regarding the nodes (graph)
        
        foreach (Waypoint node in endNodes)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(node.buildingNo, null)); // null = no images
        }
        dropdown.RefreshShownValue();
    }

    public void SetOptions()
    {
        MainManager.SelectedDestination = dropdown.options[dropdown.value].text;
    }
}
