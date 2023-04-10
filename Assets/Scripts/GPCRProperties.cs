/*  File:       GPCRProperties
    Purpose:    this file holds the functions and variables for the activation
                property of the GPCR, implementing the ActivationProperties
                Interface
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPCRProperties : MonoBehaviour , ActivationProperties
{
    #region Public Fields + Properties + Events + Delegates + Enums

    public bool m_isActive = true;

    #endregion Public Fields + Properties + Events + Delegates + Enums

    #region Public Methods

    public bool isActive
    {
        get => m_isActive;
        set => m_isActive = value;
    }

    public void changeState(bool message)
    {
        this.isActive = message;
    }

    #endregion Public Methods

    #region Private Methods

    private void Start()
    {
        changeState(false);
    }

    #endregion Private Methods
}
