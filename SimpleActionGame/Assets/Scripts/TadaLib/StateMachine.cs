using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// ステートマシンもどき
/// ステートパターンを容易に実装できるライブラリ
/// </summary>

namespace TadaLib
{
    // ステートマシンのクラス
    public class StateMachine<T>
    {
        // このステートマシンを持っているクラス
        private T parent;

        // 現在のステート
        private StateBase state_;

        // シーン名とそれに対応するシーンクラスの辞書
        private Dictionary<int, StateBase> factory_;

        // ステートを変更したいときに，要求する列挙型を入れる
        private Queue<int> state_queue_;

        // 前回のステート
        public int PrevStateId { private set; get; }


        // コンストラクタ
        public StateMachine(T data)
        {
            parent = data;
            factory_ = new Dictionary<int, StateBase>();
            state_queue_ = new Queue<int>();
        }

        // ステートを実行する
        public void Proc()
        {
            Assert.IsTrue(state_queue_.Count >= 1, "初期のステートが登録されていません");

            state_.Proc(); // ステートの状態を更新

            CheckState(); // ステートの変更要求があるか確かめる
        }

        // ステートを登録
        public void AddState(int key, StateBase value)
        {
            factory_.Add(key, value);
            value.state_machine_ = this; // ステートにマシンのデータを渡す ここ天才
        }

        public void SetInitialState(int key)
        {
            Assert.IsTrue(factory_.ContainsKey(key), "登録されていないステートです");
            Assert.IsFalse(state_queue_.Count >= 1, "すでに初期のステートが設定されています");
            state_queue_.Enqueue(key);
            state_ = factory_[state_queue_.Peek()]; // 新しいステートに変更
            state_.OnStart(); // 新しいステートの初期化
        }

        // ステートの変更要求があるか確かめる
        private void CheckState()
        {
            while (state_queue_.Count > 1)
            {
                Assert.IsTrue(factory_.ContainsKey(state_queue_.Peek()), "登録されていないステートです");
                PrevStateId = state_queue_.Dequeue(); // 現在のステートを保持
                state_.OnEnd(); // 現在のステートの終了処理
                state_ = factory_[state_queue_.Peek()]; // 新しいステートに変更
                state_.OnStart(); // 新しいステートの初期化
            }
        }

        // ステートを変更する(外部から) 強制的に選択したステートにするため，Queueは空にする
        public void ChangeState(int key)
        {
            Assert.IsTrue(factory_.ContainsKey(state_queue_.Peek()), "登録されていないステートです");
            PrevStateId = state_queue_.Dequeue();
            state_.OnEnd();
            state_queue_.Clear();
            state_queue_.Enqueue(key);
            state_ = factory_[state_queue_.Peek()]; // 新しいステートに変更
            state_.OnStart(); // 新しいステートの初期化
        }

        // 各ステートのベースとなる基底クラス
        public abstract class StateBase
        {
            // このステートが所属するステートマシン
            public StateMachine<T> state_machine_; // これもっと隠したいな
            // ステートマシンのインスタンス
            protected T Parent { get { return state_machine_.parent; } }
            // 前回のステート
            protected int PrevStateId { get { return state_machine_.PrevStateId; } }

            // ステートが始まった時に呼ばれるメソッド
            public virtual void OnStart()
            {

            }

            // ステートが終了したときに呼ばれるメソッド
            public virtual void OnEnd()
            {

            }

            // 毎フレーム呼ばれる関数
            public virtual void Proc()
            {

            }

            // ステートを変更する 第一引数に変更後のステート，第二引数には・・・
            protected void ChangeState(int new_state_id)
            {
                state_machine_.state_queue_.Enqueue(new_state_id);
            }

            // ステート開始のデバッグ出力をする これ蛇足・・・？
            protected void DumpStartMsg(StateBase state)
            {
                Debug.Log(state.GetType().Name + "開始");
            }

            // ステート終了のデバッグ出力をする
            protected void DumpEndMsg(StateBase state)
            {
                Debug.Log(state.GetType().Name + "終了");
            }
        }
    }
}