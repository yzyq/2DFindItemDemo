using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZYQ.Demo
{
    public class UILoginPanel : UIPanel
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image logoImage;
        [SerializeField] private Image lefteyeImage;
        [SerializeField] private Image righteyeImage;
        [SerializeField] private Button loginButton;

        [Header("Intro Timing")]
        [SerializeField] private float logoIntroDuration = 1.05f;
        [SerializeField] private float titleIntroDuration = 0.45f;
        [SerializeField] private float eyeIntroDuration = 0.35f;
        [SerializeField] private float buttonIntroDuration = 0.35f;

        [Header("Eye Loop")]
        [SerializeField] private float blinkInterval = 1.2f;
        [SerializeField] private float blinkDuration = 0.12f;
        [SerializeField] private float eyeLookAngle = 8f;
        [SerializeField] private float crossBlinkDelay = 0.1f;
        [SerializeField] private float winkSquash = 0.1f;
        [SerializeField] private float cuteTiltAngle = 5f;

        public event Action StartClicked;

        private readonly List<MotionHandle> motionHandles = new();
        private CanvasGroup buttonGroup;
        private Coroutine eyeLoopCoroutine;

        private Vector3 logoDefaultScale = Vector3.one;
        private Quaternion logoDefaultRotation = Quaternion.identity;
        private Vector2 titleDefaultPosition;
        private Vector3 titleDefaultScale = Vector3.one;
        private Vector3 leftEyeDefaultScale = Vector3.one;
        private Vector3 rightEyeDefaultScale = Vector3.one;
        private Quaternion leftEyeDefaultRotation = Quaternion.identity;
        private Quaternion rightEyeDefaultRotation = Quaternion.identity;
        private Vector3 buttonDefaultScale = Vector3.one;

        private void OnValidate()
        {
            Type = UIPanelType.Start;
        }

        protected override void OnShow()
        {
            StopAnimations();
            EnsureReferences();
            CacheDefaultValues();
            BindButton();
            ResetVisualState();
            PlayIntro();
        }

        protected override void OnHide()
        {
            StopAnimations();
        }

        private void OnDisable()
        {
            StopAnimations();
        }

        private void EnsureReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (loginButton != null && buttonGroup == null)
                buttonGroup = loginButton.GetComponent<CanvasGroup>() ?? loginButton.gameObject.AddComponent<CanvasGroup>();
        }

        private void CacheDefaultValues()
        {
            if (logoImage != null)
            {
                logoDefaultScale = logoImage.rectTransform.localScale;
                logoDefaultRotation = logoImage.rectTransform.localRotation;
            }

            if (titleText != null)
            {
                titleDefaultPosition = titleText.rectTransform.anchoredPosition;
                titleDefaultScale = titleText.rectTransform.localScale;
            }

            if (lefteyeImage != null)
            {
                leftEyeDefaultScale = lefteyeImage.rectTransform.localScale;
                leftEyeDefaultRotation = lefteyeImage.rectTransform.localRotation;
            }

            if (righteyeImage != null)
            {
                rightEyeDefaultScale = righteyeImage.rectTransform.localScale;
                rightEyeDefaultRotation = righteyeImage.rectTransform.localRotation;
            }

            if (loginButton != null)
                buttonDefaultScale = loginButton.transform.localScale;
        }

        private void BindButton()
        {
            if (loginButton == null) return;

            loginButton.onClick.RemoveListener(HandleStartClicked);
            loginButton.onClick.AddListener(HandleStartClicked);
        }

        private void HandleStartClicked()
        {
            StartClicked?.Invoke();
        }

        private void ResetVisualState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (logoImage != null)
            {
                logoImage.rectTransform.localScale = logoDefaultScale * 0.72f;
                logoImage.rectTransform.localRotation = logoDefaultRotation * Quaternion.Euler(0f, 0f, -8f);
                SetGraphicAlpha(logoImage, 0f);
            }

            if (titleText != null)
            {
                titleText.rectTransform.anchoredPosition = titleDefaultPosition + Vector2.down * 28f;
                titleText.rectTransform.localScale = titleDefaultScale * 0.94f;
                SetGraphicAlpha(titleText, 0f);
            }

            ResetEye(lefteyeImage, leftEyeDefaultScale, leftEyeDefaultRotation, 0f, 0.72f);
            ResetEye(righteyeImage, rightEyeDefaultScale, rightEyeDefaultRotation, 0f, 0.72f);

            if (buttonGroup != null)
            {
                buttonGroup.alpha = 0f;
                buttonGroup.interactable = false;
                buttonGroup.blocksRaycasts = false;
            }

            if (loginButton != null)
                loginButton.transform.localScale = buttonDefaultScale * 0.92f;
        }

        private void PlayIntro()
        {
            PlayLogoIntro();
            PlayTitleIntro(logoIntroDuration * 0.72f);
            PlayEyeIntro(lefteyeImage, leftEyeDefaultScale, leftEyeDefaultRotation, logoIntroDuration + 0.05f, -cuteTiltAngle);
            PlayEyeIntro(righteyeImage, rightEyeDefaultScale, rightEyeDefaultRotation, logoIntroDuration + 0.05f + crossBlinkDelay, cuteTiltAngle);
            PlayButtonIntro(logoIntroDuration + titleIntroDuration + eyeIntroDuration * 0.75f);

            AddMotion(LMotion.Create(0f, 1f, logoIntroDuration + titleIntroDuration + eyeIntroDuration + 0.1f)
                .WithOnComplete(StartEyeLoop)
                .Bind(_ => { }));
        }

        private void PlayLogoIntro()
        {
            if (logoImage == null) return;

            AddMotion(LMotion.Create(0f, 1f, logoIntroDuration * 0.7f)
                .WithEase(Ease.OutQuad)
                .BindToColorA(logoImage));

            AddMotion(LMotion.Create(logoDefaultScale * 0.72f, logoDefaultScale, logoIntroDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(logoImage.rectTransform));

            AddMotion(LMotion.Create(-8f, 0f, logoIntroDuration)
                .WithEase(Ease.OutBack)
                .Bind(value => logoImage.rectTransform.localRotation = logoDefaultRotation * Quaternion.Euler(0f, 0f, value)));
        }

        private void PlayTitleIntro(float delay)
        {
            if (titleText == null) return;

            AddMotion(LMotion.Create(0f, 1f, titleIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutQuad)
                .BindToColorA(titleText));

            AddMotion(LMotion.Create(titleDefaultPosition + Vector2.down * 28f, titleDefaultPosition, titleIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutCubic)
                .BindToAnchoredPosition(titleText.rectTransform));

            AddMotion(LMotion.Create(titleDefaultScale * 0.94f, titleDefaultScale, titleIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(titleText.rectTransform));
        }

        private void PlayEyeIntro(Image eye, Vector3 defaultScale, Quaternion defaultRotation, float delay, float tilt)
        {
            if (eye == null) return;

            AddMotion(LMotion.Create(0f, 1f, eyeIntroDuration * 0.75f)
                .WithDelay(delay)
                .WithEase(Ease.OutQuad)
                .BindToColorA(eye));

            AddMotion(LMotion.Create(defaultScale * 0.72f, defaultScale, eyeIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(eye.rectTransform));

            AddMotion(LMotion.Create(tilt, 0f, eyeIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutBack)
                .Bind(value => eye.rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, value)));
        }

        private void PlayButtonIntro(float delay)
        {
            if (loginButton == null || buttonGroup == null) return;

            AddMotion(LMotion.Create(0f, 1f, buttonIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutQuad)
                .BindToAlpha(buttonGroup));

            AddMotion(LMotion.Create(buttonDefaultScale * 0.92f, buttonDefaultScale, buttonIntroDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutBack)
                .WithOnComplete(() =>
                {
                    buttonGroup.interactable = true;
                    buttonGroup.blocksRaycasts = true;
                })
                .BindToLocalScale(loginButton.transform));
        }

        private void StartEyeLoop()
        {
            if (eyeLoopCoroutine == null && isActiveAndEnabled)
                eyeLoopCoroutine = StartCoroutine(EyeLoop());
        }

        private IEnumerator EyeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(blinkInterval);
                Blink(lefteyeImage, leftEyeDefaultScale, leftEyeDefaultRotation, -cuteTiltAngle);

                yield return new WaitForSeconds(crossBlinkDelay);
                Blink(righteyeImage, rightEyeDefaultScale, rightEyeDefaultRotation, cuteTiltAngle);

                yield return new WaitForSeconds(blinkInterval * 0.45f);
                LookAround();
            }
        }

        private void Blink(Image eye, Vector3 defaultScale, Quaternion defaultRotation, float tilt)
        {
            if (eye == null) return;

            var closedScale = new Vector3(defaultScale.x, defaultScale.y * Mathf.Clamp01(winkSquash), defaultScale.z);
            AddMotion(LMotion.Create(defaultScale, closedScale, blinkDuration)
                .WithEase(Ease.InQuad)
                .WithOnComplete(() =>
                {
                    AddMotion(LMotion.Create(closedScale, defaultScale, blinkDuration * 1.35f)
                        .WithEase(Ease.OutBack)
                        .BindToLocalScale(eye.rectTransform));
                })
                .BindToLocalScale(eye.rectTransform));

            AddMotion(LMotion.Create(0f, tilt, blinkDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() =>
                {
                    AddMotion(LMotion.Create(tilt, 0f, blinkDuration * 1.35f)
                        .WithEase(Ease.OutQuad)
                        .Bind(value => eye.rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, value)));
                })
                .Bind(value => eye.rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, value)));
        }

        private void LookAround()
        {
            RotateEye(lefteyeImage, leftEyeDefaultRotation, eyeLookAngle);
            RotateEye(righteyeImage, rightEyeDefaultRotation, -eyeLookAngle);
        }

        private void RotateEye(Image eye, Quaternion defaultRotation, float angle)
        {
            if (eye == null) return;

            AddMotion(LMotion.Create(0f, angle, 0.18f)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() =>
                {
                    AddMotion(LMotion.Create(angle, 0f, 0.32f)
                        .WithEase(Ease.OutQuad)
                        .Bind(value => eye.rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, value)));
                })
                .Bind(value => eye.rectTransform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, value)));
        }

        private static void ResetEye(Image eye, Vector3 defaultScale, Quaternion defaultRotation, float alpha, float scale)
        {
            if (eye == null) return;

            eye.rectTransform.localScale = defaultScale * scale;
            eye.rectTransform.localRotation = defaultRotation;
            SetGraphicAlpha(eye, alpha);
        }

        private void AddMotion(MotionHandle handle)
        {
            motionHandles.Add(handle);
        }

        private void StopAnimations()
        {
            if (eyeLoopCoroutine != null)
            {
                StopCoroutine(eyeLoopCoroutine);
                eyeLoopCoroutine = null;
            }

            foreach (var handle in motionHandles)
            {
                if (handle.IsActive())
                    handle.Cancel();
            }

            motionHandles.Clear();
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
