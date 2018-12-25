﻿
using System;
using System.Collections.Generic;
using UnityEngine;

namespace   App.Shared.Audio
{
    public class AKBankAtomSet
    {
        private readonly Dictionary<string, AKBankAtom> bankAtomContenter = new Dictionary<string, AKBankAtom>();
        //完整加载的bank列表
        private readonly HashSet<string> bankFinishLoadingIdList = new HashSet<string>();
        //加载中的bank列表
        private readonly HashSet<string> bankAyncLoadingIdList = new HashSet<string>();
        //  private readonly HashSet<string> bankOnLoadIdList = new HashSet<string>();
        public System.Collections.IEnumerator GetBanksEnumerator() { return bankAtomContenter.Values.GetEnumerator(); }
        internal void Recycle()
        {
            bankAtomContenter.Clear();
            bankFinishLoadingIdList.Clear();
            bankAyncLoadingIdList.Clear();

        }

        public AKBankAtomSet(AKAudioBankLoader.BankLoadHandlerAgent loader)
        {
            loader.BroadcastAsync_LoadSucessEvt += OnLoadSucess;
            loader.BroadcastAsync_LoadFailEvt += OnLoadFailed;
            loader.BroadcastAsync_UpdateRefEvt += OnLoadRefUpdate;
            loader.BroadcastAsync_UnloadFailEvt += OnUnloadFail;
            loader.BroadcastAsync_UnloadSucessEvt += OnUnloadSucess;
        }
        internal void AddLoadingStateAtom(string bankName)
        {
            bankFinishLoadingIdList.Add(bankName);
        }

        internal AKRESULT VertifyBankLoadLicense(string bankName)
        {
            if (!bankAtomContenter.ContainsKey(bankName))
                return AKRESULT.AK_UnknownBankID;
            if (bankFinishLoadingIdList.Contains(bankName))
                return AKRESULT.AK_BankAlreadyLoaded;
            if (bankAyncLoadingIdList.Contains(bankName))
                return AKRESULT.AK_BankInAyncLoading;
            return AKRESULT.AK_Success;
        }
        internal AKRESULT VertifyBankUnloadLicense(string bankName)
        {
            if (!bankAtomContenter.ContainsKey(bankName))
                return AKRESULT.AK_UnknownBankID;
            if (!bankFinishLoadingIdList.Contains(bankName))
                return AKRESULT.AK_BankNotLoadYet;
            if (bankAyncLoadingIdList.Contains(bankName))
                return AKRESULT.AK_BankInAyncLoading;
            return AKRESULT.AK_Success;
        }
        internal bool Add(AkBankRes bankData, AKAudioBankLoader.BankLoadHandlerAgent loadHandlers)
        {
            AKBankAtom atom;
            string bankName = bankData.Name;
            if (bankAtomContenter.TryGetValue(bankName, out atom)) return false;
            atom = new AKBankAtom(bankData, loadHandlers);
            bankAtomContenter.Add(bankName, atom);
            //  bankFinishLoadingIdList.Add(bankName);

            return true;
        }
        internal AKRESULT LoadSync(string bankName)
        {
            return bankAtomContenter[bankName].LoadSync();
        }
        internal AKRESULT Unload(string bankName)
        {
            return bankAtomContenter[bankName].Unload();
        }
        public List<AKAudioBankLoader.BankLoadInfo> UnloadAll()
        {
            AKRESULT result;
            List<AKAudioBankLoader.BankLoadInfo> failList= new List<AKAudioBankLoader.BankLoadInfo>();
            foreach (var name in bankFinishLoadingIdList)
            {
                result= Unload(name);
                if(result != AKRESULT.AK_Success)
                {
                    failList.Add(new AKAudioBankLoader.BankLoadInfo(result, name));
                }
               
            }
            return failList;
        }

        internal AKRESULT LoadAsync(string bankName, AkCallbackManager.BankCallback ayncLoadHandler, System.Object callbckCookie)
        {
            AKRESULT result = bankAtomContenter[bankName].LoadAsync(ayncLoadHandler, callbckCookie);
            if (result == AKRESULT.AK_WaitBankLoadingFinish)
            {
                bankAyncLoadingIdList.Add(bankName);
            }
            return result;
        }
        private void OnLoadSucess(string bankName)
        {
            bankAyncLoadingIdList.Remove(bankName);
            bankFinishLoadingIdList.Add(bankName);
        }
        private void OnLoadFailed(string bankName)
        {
            bankAyncLoadingIdList.Remove(bankName);
        }
        private void OnLoadRefUpdate(string bankName)
        {

        }
        private void OnUnloadSucess(string bankName)
        {
            bankFinishLoadingIdList.Remove(bankName);
        }
        private void OnUnloadFail(string bankName)
        {
        }

    }
    public class AKBankAtom 
    {
        public AkBankRes BankData { get; private set; }
        private AKAudioBankLoader.LoadOrUnloadInternalDelgate loadHanlder;
        private AKAudioBankLoader.LoadOrUnloadInternalDelgate unloadHandler;

        public AKBankAtom(AkBankRes data, AKAudioBankLoader.BankLoadHandlerAgent handlerAgent)
        {

            BankData = data;
            loadHanlder = handlerAgent.InternalLoadHandler;
            unloadHandler = handlerAgent.InternalUnloadHandler;
        }
        public string GetName() { return BankData.Name; }
        public List<int> GetTriggerList() { return BankData.TriggerList; }
        public List<int> GetUnLoadTriggerList() { return BankData.UnLoadTriggerList; }
        public bool InternalAsyncLoad {
            get { return BankData.LoadAsynchronous; }
        }
        public bool IsSaveDeCode
        {
            get
            {
                return BankData.LoadType == AudioBankLoadType.DecodeOnLoadAndSave;

            }
        }
        public bool IsDecode
        {
            get
            {
                return BankData.LoadType != AudioBankLoadType.Normal;
            }
        }
        //AK的机制只支持加载名字
        internal AKRESULT LoadSync()
        {
            return AkBankManagerExt.LoadBankRes(BankData.Name, IsDecode, IsSaveDeCode);
        }
        internal AKRESULT Unload()
        {
            return AkBankManagerExt.UnloadBankRes(BankData.Name);
        }
        internal AKRESULT LoadAsync(AkCallbackManager.BankCallback ayncLoadHandler, System.Object callbckCookie)
        {
            return AkBankManagerExt.LoadBankResAsync(BankData.Name, ayncLoadHandler, callbckCookie);

        }


        public void UnloadHandler(AudioTriggerArgs args)
        {
            if (unloadHandler != null)
            {
                unloadHandler(this);
            }
        }
        public void LoadHandler(AudioTriggerArgs args)
        {
            if (loadHanlder != null)
            {
                loadHanlder(this);
            }
        }

        public void Restore()
        {
        }
    }

}
