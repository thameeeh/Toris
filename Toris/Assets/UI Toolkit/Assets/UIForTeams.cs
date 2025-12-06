using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class UIForTeams : MonoBehaviour
{
    public Teams Teams;

    UIDocument uiDocument;
    ListView m_listView;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        root.Q<Label>("team__name").text = "TEAM BLUE";
        root.Q<Label>("Header").text = "Team List";

        m_listView = root.Q<ListView>("TeamList");

        m_listView.dataSource = Teams;

        m_listView.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath(nameof(Teams.teams))
        });
    }
}
