using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationController : MonoBehaviour
{
   private string _gripkey = "Grip";
   private string _triggerkey = "Trigger";
   
   [SerializeField] private InputActionProperty triggerAction;
   [SerializeField] private InputActionProperty gripAction;
   
   private Animator _animator;

   private void Start()
   {
      _animator = GetComponent<Animator>();
   }

   private void Update()
   {
      float triggerValue = triggerAction.action.ReadValue<float>();
      float gripValue = gripAction.action.ReadValue<float>();
      
      _animator.SetFloat(_gripkey, gripValue);
      _animator.SetFloat(_triggerkey, triggerValue);
   }
}
