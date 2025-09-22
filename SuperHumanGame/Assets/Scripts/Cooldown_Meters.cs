using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cooldown_Meters : MonoBehaviour
{
    public RectTransform[] Meters;
    public PlayerMovement pm;

    float barWidth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //barWidth = Meters[0].anchorMax.x;

        for (int i = 0; i < Meters.Length; i++)
        {
            barWidth = Meters[i].anchorMax.y;
        }
    }

    // Update is called once per frame
    void Update()
    {
        TeleportMeter();
        ReturnMeter();
    }

    void TeleportMeter()
    {
        pm.TeleportTimer = ((pm.TeleportTimer > pm.TeleportCooldown) ? pm.TeleportCooldown : (pm.TeleportTimer < 0) ? 0 : pm.TeleportTimer);

        float y = ((pm.TeleportTimer * (100f / pm.TeleportCooldown)) * (1f / barWidth)) / 100f;

        Meters[0].anchorMax = new Vector2(Meters[0].anchorMax.x, y);
    }

    void ReturnMeter()
    {
        pm.ReturnTimer = ((pm.ReturnTimer > pm.ReturnCooldown) ? pm.ReturnCooldown : (pm.ReturnTimer < 0) ? 0 : pm.ReturnTimer);

        float y = ((pm.ReturnTimer * (100f / pm.ReturnCooldown)) * (1f / barWidth)) / 100f;

        Meters[1].anchorMax = new Vector2(Meters[1].anchorMax.x, y);
    }
}
