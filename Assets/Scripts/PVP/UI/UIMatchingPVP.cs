using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using WE.PVP.Manager;

namespace WE.UI.PVP
{
    public class UIMatchingPVP : UIBase
    {
        [SerializeField] private UIInfoPlayerPVP _uiInfoPlayer;
        [SerializeField] private UIInfoPlayerPVP _uiInfoOrtherPlayer;

        [SerializeField] private GameObject _gButtonExit;
        [SerializeField] private GameObject _gFind;
        [SerializeField] private GameObject _gFound;
        [SerializeField] private Text _txtTime;

        private bool isMatchingSuccess;
        private IDisposable _waitDisposable;
        private int _timewait = 0;
        private const int MAX_TIME = 35;

        private const string intendTime = "Intended time ";
        public override void InitUI()
        {
            
        }
        public override void Show()
        {
            isMatchingSuccess = false;
            _gButtonExit.SetActive(true);
            _gFind.SetActive(true);
            _gFound.SetActive(false);
            _timewait = 0;
            _txtTime.text = string.Format(intendTime, MAX_TIME);

            WaitingMatching();

        }

        public void FillDataPlayer(ReadyPVPMessage data)
        {
            _uiInfoPlayer.LoadInfo(data);
        }

        public void FillDataOtherPlayer(ReadyPVPMessage data)
        {

            _waitDisposable?.Dispose();
            isMatchingSuccess = true;
            _uiInfoOrtherPlayer.LoadInfo(data);
            _gButtonExit.SetActive(false);
            _gFind.SetActive(false);
            _gFound.SetActive(true);
        }

        private void WaitingMatching()
        {
            _waitDisposable?.Dispose();
            _waitDisposable = Observable.Interval(TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale)
                                            .Subscribe(_ =>
                                            {
                                                try
                                                {
                                                    _timewait++;
                                                    if (_timewait >= MAX_TIME)
                                                    {
                                                        if (!isMatchingSuccess)
                                                        {
                                                            PVPManager.Instance.CountMatching++;
                                                            LeaveRoom();
                                                            Hide();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!isMatchingSuccess)
                                                            _txtTime.text = string.Format(intendTime, MAX_TIME - _timewait);
                                                    }
                                                }
                                                catch
                                                {

                                                }
                                            }).AddTo(this.gameObject);
        }
        private void LeaveRoom()
        {
            PVPManager.Instance.Room.LeaveRoom(false, false);
        }
    }

}