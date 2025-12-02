using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class UIForTeams : MonoBehaviour
{
    public Teams Teams;

    UIDocument uiDocument;
    ListView m_listView;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        root.Q<Label>("team__name").text = "TEAM BLUE";
        
        m_listView = root.Q<ListView>();

        m_listView.dataSource = Teams;

        m_listView.SetBinding("itemSource", new DataBinding 
        {
            dataSourcePath = new PropertyPath(nameof(Teams.teams))
        });
    }
}
