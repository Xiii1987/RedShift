using UnityEngine;

[CreateAssetMenu(
    fileName = "MAIL_SenderProfile",
    menuName = "Redshift/RRN Mail/Sender Profile")]
public class RRNMailSenderProfile : ScriptableObject
{
    [Header("Identity")]
    public string senderID;
    public string displayName;

    [Header("Department")]
    public string departmentName;
    public Sprite departmentIcon;

    [Header("Mail Defaults")]
    [Tooltip("Used when generating new email IDs, for example MAIL_IMMY_")]
    public string emailPrefix = "MAIL_";

    [TextArea(1, 3)]
    public string defaultSignOff;
}
