using System;
using System.Threading.Tasks;
using UnityEngine;
public static class FourDirectionProbe {
public static async Task<string> Run(){
InputLockManager.DialogueLock=false;InputLockManager.TransitionLock=false;InputLockManager.ESCMenuLock=false;InputLockManager.TutorialMoveLock=false;Time.timeScale=1;
var c=CameraPivotRotator.Instance;if(!c.AllowsFourDirections)throw new Exception("Four directions disabled");
int initial=(int)c.CurrentViewDirection;string result="";
foreach(int step in new[]{1,1,1,1,-1,-1,-1,-1}){
int expected=((int)c.CurrentViewDirection+step+4)%4;if(!c.TryRotateView(step))throw new Exception("Rejected");int n=0;while(c.IsRotating&&n++<1000)await Task.Delay(10);
if(c.IsRotating||(int)c.CurrentViewDirection!=expected||Quaternion.Angle(c.transform.rotation,Quaternion.Euler(c.stage3TiltAngle,180+90*expected,0))>.15f)throw new Exception("Direction mismatch");
result+=expected+" ";}
return "PASS: "+result;
}}
