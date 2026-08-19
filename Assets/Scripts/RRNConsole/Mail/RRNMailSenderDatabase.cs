using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DB_RRNMailSenders",
    menuName = "Redshift/RRN Mail/Sender Database")]
public class RRNMailSenderDatabase : ScriptableObject
{
    [SerializeField] private List<RRNMailSenderProfile> senders = new();

    public IReadOnlyList<RRNMailSenderProfile> Senders => senders;
}
