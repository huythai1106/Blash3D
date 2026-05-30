using UnityEngine;
using System.Collections;
public class CFX_AutoDestructShuriken : MonoBehaviour
{
	public bool OnlyDeactivate;
	private ParticleSystem mParticleSystem;
	WaitForSeconds wait;
	/// <summary>
	/// Awake is called when the script instance is being loaded.
	/// </summary>
	void Awake()
	{
		mParticleSystem = gameObject.GetComponent<ParticleSystem>();
		wait = new WaitForSeconds(0.5f);
	}
	
	void OnEnable()
	{
		StartCoroutine("CheckIfAlive");
	}
	
	IEnumerator CheckIfAlive ()
	{
		while(true)
		{
			yield return wait;
			if(!mParticleSystem.IsAlive())
			{
				if(OnlyDeactivate)
				{
					#if UNITY_3_5
						this.gameObject.SetActiveRecursively(false);
					#else
						this.gameObject.SetActive(false);
					#endif
				}
				else
					GameObject.Destroy(this.gameObject);
				break;
			}
		}
	}
}
