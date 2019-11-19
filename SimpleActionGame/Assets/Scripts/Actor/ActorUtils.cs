using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actorクラスに関するヘルパークラス
/// </summary>

namespace Actor
{
    public class ActorUtils : MonoBehaviour
    {
        // 速度に加速度を加える ただし，一定の速度を超えていたら加算しない
        public static void AddAccel(ref Vector2 velocity, Vector2 accel, Vector2 max_abs_speed)
        {
            if(accel.x > 0 && velocity.x < max_abs_speed.x) velocity = new Vector2(Mathf.Min(velocity.x + accel.x, max_abs_speed.x), velocity.y);
            else if(accel.x < 0 && velocity.x > -max_abs_speed.x) velocity = new Vector2(Mathf.Max(velocity.x + accel.x, -max_abs_speed.x), velocity.y);

            if (accel.y > 0 && velocity.y < max_abs_speed.y) velocity = new Vector2(velocity.x, Mathf.Min(velocity.y + accel.y, max_abs_speed.y));
            else if (accel.y < 0 && velocity.y > -max_abs_speed.y) velocity = new Vector2(velocity.x, Mathf.Max(velocity.y + accel.y, -max_abs_speed.y));
        }
    }
}