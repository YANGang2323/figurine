using UnityEngine;

public class TimingInteractable : MonoBehaviour
{
    public bool isActivated = false;

    [Header("Outline")]
    public Outline outline;

    [Header("Effects")]
    public ParticleSystem successParticle;
    //public AudioSource audioSource;
    //public AudioClip successClip;

    void Start()
    {
        if (outline == null)
            outline = GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = false;

            // Quick Outline 설정
            outline.OutlineWidth = 1f;
        }
    }

    // 🎯 타겟팅
    public void SetTarget(bool active)
    {
        if (isActivated) return;

        if (outline != null)
            outline.enabled = active;
    }

    // ✅ 성공
    public virtual void Activate()
    {
        if (isActivated) return;

        isActivated = true;

        // Outline 제거
        if (outline != null)
            outline.enabled = false;

        // 파티클 재생
        if (successParticle != null)
            successParticle.Play();

        // 띵 소리
        //if (audioSource != null && successClip != null)
        //    audioSource.PlayOneShot(successClip);
    }
}