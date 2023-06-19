using System;
using ArouseBlockchain.UI;
using UnityEngine;

public interface IUISolveParent
{
    public void InitSolvePage();

    public bool IsUsed { get; }

    public void OpenSolve(UI_SolveParent prevSolve, IUIPage prevPage = null);

    public void HideSolve();
}

