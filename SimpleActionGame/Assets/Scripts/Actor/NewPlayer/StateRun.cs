using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.NewPlayer;
using TadaLib;

/// <summary>
/// プレイヤーの走り状態を管理するステート
/// </summary>

namespace Actor.NewPlayer
{
    public partial class Player
    {
        [System.Serializable]
        private class StateRun : StateMachine<Player>.StateBase
        {
            // ステート間で共有するデータのコピーインスタンス
            private Data data = null;

            [SerializeField]
            private Vector2 max_speed = new Vector2(0.26f, 0f);
            [SerializeField]
            private Vector2 accel = new Vector2(0.017f, 0f);

            // ステートが始まった時に呼ばれるメソッド
            public override void OnStart()
            {
                if(data == null) data = Parent.data_;

                // 走りアニメーション開始
                data.animator.Play("Walk");

                // 移動している方向に速度を加える
                data.velocity = new Vector2(data.velocity.x, 0f);
            }

            // ステートが終了したときに呼ばれるメソッド
            public override void OnEnd()
            {

            }

            // 毎フレーム呼ばれる関数
            public override void Proc()
            {
                // ジャンプ入力ならジャンプステートへ
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ChangeState((int)eState.Jump);
                    return;
                }

                // 地面から離れたらふぉるステートへ
                if (!data.IsGround)
                {
                    ChangeState((int)eState.Fall);
                }

                // 移動している方向に速度を加える
                float dir = 0f;
                if (Input.GetKey(KeyCode.LeftArrow)) dir = -1f;
                else if (Input.GetKey(KeyCode.RightArrow)) dir = 1f;
                else
                {
                    // 何も押していないならWait状態に
                    ChangeState((int)eState.Wait);
                    return;
                }

                ActorUtils.AddAccel(ref data.velocity, new Vector2(dir, 1f) * accel * Time.deltaTime * 60f, max_speed);
            }
        }
    }
}