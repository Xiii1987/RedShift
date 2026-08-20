using System;
using System.Collections.Generic;
using UnityEngine;

public enum RRNEmailDeliveryType
{
    Manual,
    RandomFluff
}

public enum RRNEmailResponseAction
{
    None,
    SendFollowUpEmail,
    StartProgramme,
    AddMoney,
    AddResearchPoints
}

public enum RRNEmailFollowUpTiming
{
    Immediate,
    AfterDelay,
    NextWorkingDay
}

[Serializable]
public class RRNEmailResponseDefinition
{
    [TextArea(1, 2)]
    public string buttonText;

    public RRNEmailResponseAction action = RRNEmailResponseAction.None;

    [Tooltip("Email ID, Programme ID, etc. depending on the selected action.")]
    public string targetID;

    [Tooltip("Used by actions such as AddMoney or AddResearchPoints.")]
    public int amount;

    [Header("Follow-Up Timing")]
    [Tooltip("Only used when the action is SendFollowUpEmail.")]
    public RRNEmailFollowUpTiming followUpTiming = RRNEmailFollowUpTiming.Immediate;

    [Min(1)]
    [Tooltip("In-game minutes to wait when Follow Up Timing is After Delay.")]
    public int followUpDelayMinutes = 60;
}

[Serializable]
public class RRNEmailDefinition
{
    [Header("Identity")]
    public string emailID;

    [Header("Delivery")]
    public RRNEmailDeliveryType deliveryType = RRNEmailDeliveryType.Manual;

    [Tooltip("Normally leave this off. Random fluff should not repeat.")]
    public bool allowRepeat = false;

    [Header("Sender")]
    public string sender;
    public string departmentName;
    public Sprite departmentIcon;

    [Header("Message")]
    public string subject;

    [TextArea(6, 20)]
    public string body;

    [Header("Responses - Maximum 3")]
    public List<RRNEmailResponseDefinition> responses = new();
}

[CreateAssetMenu(
    fileName = "DB_RRNEmail",
    menuName = "Redshift/RRN Email Database")]
public class RRNEmailDatabase : ScriptableObject
{
    [SerializeField] private List<RRNEmailDefinition> emails = new();

    public IReadOnlyList<RRNEmailDefinition> Emails => emails;

    public RRNEmailDefinition GetEmailByID(string emailID)
    {
        if (string.IsNullOrWhiteSpace(emailID))
            return null;

        foreach (RRNEmailDefinition email in emails)
        {
            if (email != null && email.emailID == emailID)
                return email;
        }

        return null;
    }
}
