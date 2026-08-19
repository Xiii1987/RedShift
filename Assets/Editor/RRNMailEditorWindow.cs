using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RRNMailEditorWindow : EditorWindow
{
    private enum Tab
    {
        Profile,
        Emails,
        Links
    }

    private RRNEmailDatabase emailDatabase;
    private RRNMailSenderDatabase senderDatabase;

    private SerializedObject emailDatabaseSO;
    private SerializedProperty emailsProperty;

    private SerializedObject senderDatabaseSO;
    private SerializedProperty sendersProperty;

    private int selectedSenderIndex = -1;
    private int selectedEmailIndex = -1;
    private Tab currentTab = Tab.Emails;

    private Vector2 leftScroll;
    private Vector2 rightScroll;
    private string emailSearch = string.Empty;

    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;

    [MenuItem("Redshift/RRN/Mail Editor")]
    public static void Open()
    {
        RRNMailEditorWindow window = GetWindow<RRNMailEditorWindow>("RRN Mail Editor");
        window.minSize = new Vector2(900f, 560f);
        window.Show();
    }

    private void OnEnable()
    {
        FindDefaultDatabases();
        RefreshSerializedObjects();
    }

    private void OnGUI()
    {
        EnsureStyles();

        DrawDatabaseBar();

        if (emailDatabase == null || senderDatabase == null)
        {
            EditorGUILayout.HelpBox(
                "Assign both the RRN Email Database and Sender Database to begin.",
                MessageType.Info);
            return;
        }

        RefreshSerializedObjectsIfNeeded();
        DrawSenderBar();

        RRNMailSenderProfile sender = GetSelectedSender();
        if (sender == null)
        {
            EditorGUILayout.HelpBox(
                "Create or select a sender profile. Emails are filtered by the selected sender.",
                MessageType.Info);
            return;
        }

        currentTab = (Tab)GUILayout.Toolbar(
            (int)currentTab,
            new[] { "Sender Profile", "Emails", "Links" },
            GUILayout.Height(26f));

        EditorGUILayout.Space(6f);

        switch (currentTab)
        {
            case Tab.Profile:
                DrawProfileTab(sender);
                break;

            case Tab.Emails:
                DrawEmailsTab(sender);
                break;

            case Tab.Links:
                DrawLinksTab(sender);
                break;
        }
    }

    private void DrawDatabaseBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("RRN MAIL EDITOR", headerStyle);

        EditorGUI.BeginChangeCheck();

        emailDatabase = (RRNEmailDatabase)EditorGUILayout.ObjectField(
            "Email Database",
            emailDatabase,
            typeof(RRNEmailDatabase),
            false);

        senderDatabase = (RRNMailSenderDatabase)EditorGUILayout.ObjectField(
            "Sender Database",
            senderDatabase,
            typeof(RRNMailSenderDatabase),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            selectedSenderIndex = -1;
            selectedEmailIndex = -1;
            RefreshSerializedObjects();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSenderBar()
    {
        senderDatabaseSO.Update();

        List<RRNMailSenderProfile> senders = GetSenders();
        string[] senderNames = new string[senders.Count];

        for (int i = 0; i < senders.Count; i++)
        {
            RRNMailSenderProfile sender = senders[i];
            senderNames[i] = sender != null
                ? (!string.IsNullOrWhiteSpace(sender.displayName)
                    ? sender.displayName
                    : sender.name)
                : "<Missing Sender>";
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Sender", GUILayout.Width(48f));

        int newIndex = selectedSenderIndex;

        if (senders.Count == 0)
        {
            GUILayout.Label("No sender profiles", EditorStyles.miniLabel);
            newIndex = -1;
        }
        else
        {
            if (newIndex < 0 || newIndex >= senders.Count)
                newIndex = 0;

            newIndex = EditorGUILayout.Popup(newIndex, senderNames);
        }

        if (newIndex != selectedSenderIndex)
        {
            selectedSenderIndex = newIndex;
            selectedEmailIndex = -1;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ New Sender", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            CreateSenderProfile();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawProfileTab(RRNMailSenderProfile sender)
    {
        EditorGUILayout.LabelField("SENDER PROFILE", subHeaderStyle);
        EditorGUILayout.Space(4f);

        SerializedObject senderSO = new SerializedObject(sender);
        senderSO.Update();

        EditorGUILayout.PropertyField(senderSO.FindProperty("senderID"));
        EditorGUILayout.PropertyField(senderSO.FindProperty("displayName"));
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(senderSO.FindProperty("departmentName"));
        EditorGUILayout.PropertyField(senderSO.FindProperty("departmentIcon"));
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(senderSO.FindProperty("emailPrefix"));
        EditorGUILayout.PropertyField(senderSO.FindProperty("defaultSignOff"));

        if (senderSO.ApplyModifiedProperties())
            EditorUtility.SetDirty(sender);

        EditorGUILayout.Space(12f);

        if (GUILayout.Button("Apply Profile To All Existing Emails From This Sender", GUILayout.Height(28f)))
        {
            if (EditorUtility.DisplayDialog(
                    "Apply Sender Profile",
                    "Update sender name, department and icon on every email currently assigned to this sender?",
                    "Apply",
                    "Cancel"))
            {
                ApplyProfileToExistingEmails(sender);
            }
        }
    }

    private void DrawEmailsTab(RRNMailSenderProfile sender)
    {
        EditorGUILayout.BeginHorizontal();

        DrawEmailList(sender);
        GUILayout.Space(6f);
        DrawEmailDetails(sender);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawEmailList(RRNMailSenderProfile sender)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(320f), GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("EMAILS", subHeaderStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ New Email", GUILayout.Width(92f), GUILayout.Height(22f)))
            CreateEmail(sender);

        EditorGUILayout.EndHorizontal();

        emailSearch = EditorGUILayout.TextField("Search", emailSearch);
        EditorGUILayout.Space(4f);

        List<int> filtered = GetEmailIndicesForSender(sender, emailSearch);

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox("No emails for this sender.", MessageType.None);
        }
        else
        {
            foreach (int emailIndex in filtered)
            {
                SerializedProperty email = emailsProperty.GetArrayElementAtIndex(emailIndex);
                string id = email.FindPropertyRelative("emailID").stringValue;
                string subject = email.FindPropertyRelative("subject").stringValue;

                bool selected = selectedEmailIndex == emailIndex;

                GUIStyle style = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fixedHeight = 42f,
                    wordWrap = true
                };

                string label = string.IsNullOrWhiteSpace(subject)
                    ? id
                    : $"{subject}\n{id}";

                if (GUILayout.Toggle(selected, label, style) && !selected)
                    selectedEmailIndex = emailIndex;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEmailDetails(RRNMailSenderProfile sender)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("EMAIL DETAILS", subHeaderStyle);

        if (!IsSelectedEmailValid())
        {
            EditorGUILayout.HelpBox("Select an email from the list, or create a new one.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        emailDatabaseSO.Update();
        SerializedProperty email = emailsProperty.GetArrayElementAtIndex(selectedEmailIndex);

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        EditorGUILayout.PropertyField(email.FindPropertyRelative("emailID"), new GUIContent("Email ID"));

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Sender", sender.displayName);
            EditorGUILayout.TextField("Department", sender.departmentName);
            EditorGUILayout.ObjectField("Department Icon", sender.departmentIcon, typeof(Sprite), false);
        }

        if (GUILayout.Button("Reapply Sender Defaults", GUILayout.Width(170f)))
            ApplySenderToEmail(email, sender);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Delivery", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(email.FindPropertyRelative("deliveryType"));
        EditorGUILayout.PropertyField(email.FindPropertyRelative("allowRepeat"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(email.FindPropertyRelative("subject"));
        EditorGUILayout.PropertyField(email.FindPropertyRelative("body"));

        EditorGUILayout.Space(10f);
        DrawResponses(email, sender);

        EditorGUILayout.EndScrollView();

        if (emailDatabaseSO.ApplyModifiedProperties())
            EditorUtility.SetDirty(emailDatabase);

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Duplicate Email", GUILayout.Height(24f)))
            DuplicateSelectedEmail(sender);

        GUI.backgroundColor = new Color(1f, 0.55f, 0.45f);
        if (GUILayout.Button("Delete Email", GUILayout.Height(24f)))
            DeleteSelectedEmail();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawResponses(SerializedProperty email, RRNMailSenderProfile sender)
    {
        EditorGUILayout.LabelField("RESPONSES", EditorStyles.boldLabel);

        SerializedProperty responses = email.FindPropertyRelative("responses");

        if (responses.arraySize > 3)
            responses.arraySize = 3;

        for (int i = 0; i < responses.arraySize; i++)
        {
            SerializedProperty response = responses.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Response {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                responses.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(response.FindPropertyRelative("buttonText"), new GUIContent("Button Text"));
            EditorGUILayout.PropertyField(response.FindPropertyRelative("action"));

            RRNEmailResponseAction action =
                (RRNEmailResponseAction)response.FindPropertyRelative("action").enumValueIndex;

            SerializedProperty targetID = response.FindPropertyRelative("targetID");
            SerializedProperty amount = response.FindPropertyRelative("amount");

            switch (action)
            {
                case RRNEmailResponseAction.SendFollowUpEmail:
                    DrawEmailTargetPopup(targetID);
                    break;

                case RRNEmailResponseAction.StartProgramme:
                    EditorGUILayout.PropertyField(targetID, new GUIContent("Programme ID"));
                    break;

                case RRNEmailResponseAction.AddMoney:
                case RRNEmailResponseAction.AddResearchPoints:
                    EditorGUILayout.PropertyField(amount);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        if (responses.arraySize < 3)
        {
            if (GUILayout.Button("+ Add Response", GUILayout.Height(24f)))
            {
                int index = responses.arraySize;
                responses.InsertArrayElementAtIndex(index);
                SerializedProperty response = responses.GetArrayElementAtIndex(index);
                response.FindPropertyRelative("buttonText").stringValue = string.Empty;
                response.FindPropertyRelative("action").enumValueIndex = 0;
                response.FindPropertyRelative("targetID").stringValue = string.Empty;
                response.FindPropertyRelative("amount").intValue = 0;
            }
        }
    }

    private void DrawEmailTargetPopup(SerializedProperty targetID)
    {
        List<string> ids = new List<string> { "<None>" };
        List<string> labels = new List<string> { "<None>" };

        for (int i = 0; i < emailsProperty.arraySize; i++)
        {
            SerializedProperty candidate = emailsProperty.GetArrayElementAtIndex(i);
            string id = candidate.FindPropertyRelative("emailID").stringValue;
            string sender = candidate.FindPropertyRelative("sender").stringValue;
            string subject = candidate.FindPropertyRelative("subject").stringValue;

            if (string.IsNullOrWhiteSpace(id))
                continue;

            ids.Add(id);
            labels.Add($"{sender} > {subject}  [{id}]");
        }

        int current = Mathf.Max(0, ids.IndexOf(targetID.stringValue));
        int chosen = EditorGUILayout.Popup("Follow-Up Email", current, labels.ToArray());
        targetID.stringValue = chosen <= 0 ? string.Empty : ids[chosen];
    }

    private void DrawLinksTab(RRNMailSenderProfile sender)
    {
        EditorGUILayout.LabelField("EMAIL LINKS", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Read-only overview of reply chains. Follow-up responses are shown as links so you can spot broken or hard-to-find chains quickly.",
            MessageType.Info);

        List<int> indices = GetEmailIndicesForSender(sender, string.Empty);

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        foreach (int index in indices)
        {
            SerializedProperty email = emailsProperty.GetArrayElementAtIndex(index);
            string emailID = email.FindPropertyRelative("emailID").stringValue;
            string subject = email.FindPropertyRelative("subject").stringValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(subject, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(emailID, EditorStyles.miniLabel);

            SerializedProperty responses = email.FindPropertyRelative("responses");
            bool foundLink = false;

            for (int r = 0; r < responses.arraySize; r++)
            {
                SerializedProperty response = responses.GetArrayElementAtIndex(r);
                RRNEmailResponseAction action =
                    (RRNEmailResponseAction)response.FindPropertyRelative("action").enumValueIndex;

                if (action != RRNEmailResponseAction.SendFollowUpEmail)
                    continue;

                foundLink = true;
                string button = response.FindPropertyRelative("buttonText").stringValue;
                string target = response.FindPropertyRelative("targetID").stringValue;
                RRNEmailDefinition targetEmail = emailDatabase.GetEmailByID(target);

                string destination = targetEmail != null
                    ? $"{targetEmail.sender} > {targetEmail.subject} [{target}]"
                    : string.IsNullOrWhiteSpace(target)
                        ? "<No target>"
                        : $"<Missing: {target}>";

                EditorGUILayout.LabelField($"↳ {button}  →  {destination}", EditorStyles.wordWrappedLabel);
            }

            if (!foundLink)
                EditorGUILayout.LabelField("No follow-up email links.", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateSenderProfile()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Mail Sender Profile",
            "MAIL_NewSender",
            "asset",
            "Choose where to save the sender profile.");

        if (string.IsNullOrWhiteSpace(path))
            return;

        RRNMailSenderProfile profile = CreateInstance<RRNMailSenderProfile>();
        profile.senderID = "SENDER_NEW";
        profile.displayName = "New Sender";
        profile.emailPrefix = "MAIL_NEW_";

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();

        senderDatabaseSO.Update();
        int index = sendersProperty.arraySize;
        sendersProperty.InsertArrayElementAtIndex(index);
        sendersProperty.GetArrayElementAtIndex(index).objectReferenceValue = profile;
        senderDatabaseSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(senderDatabase);

        selectedSenderIndex = index;
        selectedEmailIndex = -1;
        Selection.activeObject = profile;
    }

    private void CreateEmail(RRNMailSenderProfile sender)
    {
        emailDatabaseSO.Update();

        int index = emailsProperty.arraySize;
        emailsProperty.InsertArrayElementAtIndex(index);

        SerializedProperty email = emailsProperty.GetArrayElementAtIndex(index);
        ClearEmail(email);
        ApplySenderToEmail(email, sender);

        email.FindPropertyRelative("emailID").stringValue = GenerateNextEmailID(sender);
        email.FindPropertyRelative("deliveryType").enumValueIndex = (int)RRNEmailDeliveryType.Manual;

        emailDatabaseSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(emailDatabase);

        selectedEmailIndex = index;
    }

    private void DuplicateSelectedEmail(RRNMailSenderProfile sender)
    {
        if (!IsSelectedEmailValid())
            return;

        emailDatabaseSO.Update();

        SerializedProperty source = emailsProperty.GetArrayElementAtIndex(selectedEmailIndex);
        int newIndex = emailsProperty.arraySize;
        emailsProperty.InsertArrayElementAtIndex(newIndex);
        SerializedProperty copy = emailsProperty.GetArrayElementAtIndex(newIndex);

        copy.FindPropertyRelative("emailID").stringValue = GenerateNextEmailID(sender);
        copy.FindPropertyRelative("deliveryType").enumValueIndex = source.FindPropertyRelative("deliveryType").enumValueIndex;
        copy.FindPropertyRelative("allowRepeat").boolValue = source.FindPropertyRelative("allowRepeat").boolValue;
        copy.FindPropertyRelative("sender").stringValue = source.FindPropertyRelative("sender").stringValue;
        copy.FindPropertyRelative("departmentName").stringValue = source.FindPropertyRelative("departmentName").stringValue;
        copy.FindPropertyRelative("departmentIcon").objectReferenceValue = source.FindPropertyRelative("departmentIcon").objectReferenceValue;
        copy.FindPropertyRelative("subject").stringValue = source.FindPropertyRelative("subject").stringValue + " COPY";
        copy.FindPropertyRelative("body").stringValue = source.FindPropertyRelative("body").stringValue;

        SerializedProperty sourceResponses = source.FindPropertyRelative("responses");
        SerializedProperty copyResponses = copy.FindPropertyRelative("responses");
        copyResponses.arraySize = sourceResponses.arraySize;

        for (int i = 0; i < sourceResponses.arraySize; i++)
        {
            SerializedProperty src = sourceResponses.GetArrayElementAtIndex(i);
            SerializedProperty dst = copyResponses.GetArrayElementAtIndex(i);

            dst.FindPropertyRelative("buttonText").stringValue = src.FindPropertyRelative("buttonText").stringValue;
            dst.FindPropertyRelative("action").enumValueIndex = src.FindPropertyRelative("action").enumValueIndex;
            dst.FindPropertyRelative("targetID").stringValue = src.FindPropertyRelative("targetID").stringValue;
            dst.FindPropertyRelative("amount").intValue = src.FindPropertyRelative("amount").intValue;
        }

        emailDatabaseSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(emailDatabase);
        selectedEmailIndex = newIndex;
    }

    private void DeleteSelectedEmail()
    {
        if (!IsSelectedEmailValid())
            return;

        SerializedProperty email = emailsProperty.GetArrayElementAtIndex(selectedEmailIndex);
        string id = email.FindPropertyRelative("emailID").stringValue;

        if (!EditorUtility.DisplayDialog(
                "Delete Email",
                $"Delete '{id}' from the database?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        emailDatabaseSO.Update();
        emailsProperty.DeleteArrayElementAtIndex(selectedEmailIndex);
        emailDatabaseSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(emailDatabase);
        selectedEmailIndex = -1;
    }

    private void ApplyProfileToExistingEmails(RRNMailSenderProfile sender)
    {
        emailDatabaseSO.Update();

        for (int i = 0; i < emailsProperty.arraySize; i++)
        {
            SerializedProperty email = emailsProperty.GetArrayElementAtIndex(i);
            string currentSender = email.FindPropertyRelative("sender").stringValue;

            if (!string.Equals(currentSender, sender.displayName, StringComparison.OrdinalIgnoreCase))
                continue;

            ApplySenderToEmail(email, sender);
        }

        emailDatabaseSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(emailDatabase);
    }

    private static void ApplySenderToEmail(SerializedProperty email, RRNMailSenderProfile sender)
    {
        email.FindPropertyRelative("sender").stringValue = sender.displayName;
        email.FindPropertyRelative("departmentName").stringValue = sender.departmentName;
        email.FindPropertyRelative("departmentIcon").objectReferenceValue = sender.departmentIcon;
    }

    private static void ClearEmail(SerializedProperty email)
    {
        email.FindPropertyRelative("emailID").stringValue = string.Empty;
        email.FindPropertyRelative("allowRepeat").boolValue = false;
        email.FindPropertyRelative("sender").stringValue = string.Empty;
        email.FindPropertyRelative("departmentName").stringValue = string.Empty;
        email.FindPropertyRelative("departmentIcon").objectReferenceValue = null;
        email.FindPropertyRelative("subject").stringValue = string.Empty;
        email.FindPropertyRelative("body").stringValue = string.Empty;
        email.FindPropertyRelative("responses").arraySize = 0;
    }

    private string GenerateNextEmailID(RRNMailSenderProfile sender)
    {
        string prefix = string.IsNullOrWhiteSpace(sender.emailPrefix)
            ? "MAIL_"
            : sender.emailPrefix;

        int highest = 0;

        for (int i = 0; i < emailsProperty.arraySize; i++)
        {
            string id = emailsProperty
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("emailID")
                .stringValue;

            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = id.Substring(prefix.Length);
            if (int.TryParse(suffix, out int number))
                highest = Mathf.Max(highest, number);
        }

        return $"{prefix}{highest + 1:000}";
    }

    private List<int> GetEmailIndicesForSender(RRNMailSenderProfile sender, string search)
    {
        List<int> result = new List<int>();

        if (sender == null || emailsProperty == null)
            return result;

        emailDatabaseSO.Update();

        for (int i = 0; i < emailsProperty.arraySize; i++)
        {
            SerializedProperty email = emailsProperty.GetArrayElementAtIndex(i);
            string senderName = email.FindPropertyRelative("sender").stringValue;

            if (!string.Equals(senderName, sender.displayName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string id = email.FindPropertyRelative("emailID").stringValue;
                string subject = email.FindPropertyRelative("subject").stringValue;

                if (id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    subject.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
            }

            result.Add(i);
        }

        return result;
    }

    private RRNMailSenderProfile GetSelectedSender()
    {
        List<RRNMailSenderProfile> senders = GetSenders();

        if (selectedSenderIndex < 0 || selectedSenderIndex >= senders.Count)
            return null;

        return senders[selectedSenderIndex];
    }

    private List<RRNMailSenderProfile> GetSenders()
    {
        List<RRNMailSenderProfile> result = new List<RRNMailSenderProfile>();

        if (sendersProperty == null)
            return result;

        senderDatabaseSO.Update();

        for (int i = 0; i < sendersProperty.arraySize; i++)
        {
            result.Add(
                sendersProperty.GetArrayElementAtIndex(i).objectReferenceValue
                as RRNMailSenderProfile);
        }

        return result;
    }

    private bool IsSelectedEmailValid()
    {
        return emailsProperty != null &&
               selectedEmailIndex >= 0 &&
               selectedEmailIndex < emailsProperty.arraySize;
    }

    private void FindDefaultDatabases()
    {
        if (emailDatabase == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:RRNEmailDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                emailDatabase = AssetDatabase.LoadAssetAtPath<RRNEmailDatabase>(path);
            }
        }

        if (senderDatabase == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:RRNMailSenderDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                senderDatabase = AssetDatabase.LoadAssetAtPath<RRNMailSenderDatabase>(path);
            }
        }
    }

    private void RefreshSerializedObjectsIfNeeded()
    {
        if ((emailDatabaseSO == null && emailDatabase != null) ||
            (senderDatabaseSO == null && senderDatabase != null))
        {
            RefreshSerializedObjects();
        }
    }

    private void RefreshSerializedObjects()
    {
        emailDatabaseSO = emailDatabase != null
            ? new SerializedObject(emailDatabase)
            : null;

        emailsProperty = emailDatabaseSO != null
            ? emailDatabaseSO.FindProperty("emails")
            : null;

        senderDatabaseSO = senderDatabase != null
            ? new SerializedObject(senderDatabase)
            : null;

        sendersProperty = senderDatabaseSO != null
            ? senderDatabaseSO.FindProperty("senders")
            : null;
    }

    private void EnsureStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };
        }
    }
}
