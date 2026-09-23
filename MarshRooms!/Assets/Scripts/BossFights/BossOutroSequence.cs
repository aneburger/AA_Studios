using System.Collections;
using UnityEngine;

public abstract class BossOutroSequence : MonoBehaviour
{
    public abstract IEnumerator Play(BossRoomController room);
}