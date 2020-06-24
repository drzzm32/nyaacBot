using System;
using System.Threading;

using static Native.Csharp.App.Event.Event_GroupMsg;

namespace dotNSASM
{
    public partial class NSASM
    {
        private Random random = new Random();

        protected virtual void LoadParamList()
        {
            paramList.Add("null", (reg) => {
                Register res = new Register();
                res.type = RegType.STR;
                res.data = "null";
                return res;
            });
            paramList.Add("rand", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.FLOAT;
                    res.readOnly = true;
                    res.data = (float)random.NextDouble();
                    return res;
                }
                return reg;
            });
            paramList.Add("cinc", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.CHAR;
                    if (funcList["in"].Invoke(res, null, null) != Result.OK)
                        return null;
                    res.readOnly = true;
                    return res;
                }
                return reg;
            });
            paramList.Add("cini", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.INT;
                    if (funcList["in"].Invoke(res, null, null) != Result.OK)
                        return null;
                    res.readOnly = true;
                    return res;
                }
                return reg;
            });
            paramList.Add("cinf", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.FLOAT;
                    if (funcList["in"].Invoke(res, null, null) != Result.OK)
                        return null;
                    res.readOnly = true;
                    return res;
                }
                return reg;
            });
            paramList.Add("cins", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.STR;
                    if (funcList["in"].Invoke(res, null, null) != Result.OK)
                        return null;
                    res.readOnly = true;
                    return res;
                }
                return reg;
            });
            paramList.Add("cin", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.STR;
                    if (funcList["in"].Invoke(res, null, null) != Result.OK)
                        return null;
                    res.readOnly = true;
                    return res;
                }
                return reg;
            });
            paramList.Add("cout", (reg) => {
                if (reg == null) return new Register();
                funcList["out"].Invoke(reg, null, null);
                return reg;
            });
            paramList.Add("cprt", (reg) => {
                if (reg == null) return new Register();
                funcList["prt"].Invoke(reg, null, null);
                return reg;
            });
            paramList.Add("arg", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    if (argReg == null)
                    {
                        res.type = RegType.STR;
                        res.readOnly = true;
                        res.data = "null";
                    }
                    else
                        res.Copy(argReg);
                    return res;
                }
                return reg;
            });
            paramList.Add("tid", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.INT;
                    res.readOnly = true;
                    res.data = Thread.CurrentThread.ManagedThreadId;
                    return res;
                }
                return reg;
            });

            paramList.Add("ver", (reg) => {
                if (reg == null)
                {
                    Register res = new Register();
                    res.type = RegType.STR;
                    res.readOnly = true;
                    res.data = Version;
                    return res;
                }
                return reg;
            });

            paramList.Add("rep", (reg) => {
                if (reg == null) return new Register();
                if (reg.type == RegType.INT)
                {
                    int val = (int)reg.data;
                    long id = GetGroupID();
                    SetRepeatEnb(id, val > 0);
                }
                return reg;
            });
            paramList.Add("lim", (reg) => {
                if (reg == null) return new Register();
                if (reg.type == RegType.FLOAT)
                {
                    float val = (float)reg.data;
                    long id = GetGroupID();
                    SetRepeatLim(id, val);
                }
                return reg;
            });
        }

        private long groupID = 0;
        public void SetGroupID(long id) { groupID = id; }
        protected long GetGroupID() { return groupID; }
    }
}
