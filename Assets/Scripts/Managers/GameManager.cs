﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Borodar.LD34.Managers
{
    public class GameManager : MonoBehaviour
    {
        private const string PREF_HIGHSCORE = "Higschore";
        public Canvas CanvasPrefab;

        public float FadeDuration = 0.5f;
        public float FadeDelay = 1f;

        public bool IsFirstRun = true;
        public int HighScore;

        private Canvas _canvasObject;
        private CanvasGroup _canvasGroup;
        private bool _isLoadingLevel;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        [SuppressMessage("ReSharper", "InvertIf")]
        private Canvas CanvasObject
        {
            get
            {
                if (_canvasObject == null)
                {
                    _canvasObject =
                        Instantiate(CanvasPrefab, CanvasPrefab.transform.position, Quaternion.identity) as Canvas;
                    DontDestroyOnLoad(_canvasObject);
                }

                return _canvasObject;
            }
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingExpression")]
        private CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = CanvasObject.GetComponent<CanvasGroup>();

                return _canvasGroup;
            }
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        public void Awake()
        {
            LoadGameData();
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void Pause()
        {
            Time.timeScale = 0;
        }

        public void Unpause()
        {
            Time.timeScale = 1f;
        }

        public void LoadScene(string levelName)
        {
            if (!_isLoadingLevel)
                StartCoroutine(LoadLevelWithFadeEffect(levelName));
        }

        public void LoadGameData()
        {
            HighScore = PlayerPrefs.GetInt(PREF_HIGHSCORE, 0);
        }

        public void SaveGameData()
        {
            PlayerPrefs.SetInt(PREF_HIGHSCORE, HighScore);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private IEnumerator LoadLevelWithFadeEffect(string levelName)
        {
            CanvasObject.gameObject.SetActive(true);
            _isLoadingLevel = true;

            // Fade in
            yield return StartCoroutine(TickAlphaInCanvasGroup(FadeDuration, false));

            // Load nextlevel
            var startLoading = Time.realtimeSinceStartup;
            Application.LoadLevel(levelName);
            yield return StartCoroutine(WaitForLevelToLoad(levelName));

            // Add delay if needed
            var realFadeDelay = FadeDelay + startLoading - Time.realtimeSinceStartup;
            if (realFadeDelay > 0)
            {
                yield return new WaitForSeconds(realFadeDelay);
            }

            // Fade out
            yield return StartCoroutine(TickAlphaInCanvasGroup(FadeDuration, true));

            CanvasObject.gameObject.SetActive(false);
            _isLoadingLevel = false;
        }

        private IEnumerator TickAlphaInCanvasGroup(float duration, bool reverseDirection)
        {
            var start = reverseDirection ? 1f : 0f;
            var end = reverseDirection ? 0f : 1f;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var step = Mathf.Lerp(start, end, Mathf.Pow(elapsed / duration, 2f));
                CanvasGroup.alpha = step;

                yield return null;
            }
        }

        private static IEnumerator WaitForLevelToLoad(string levelName)
        {
            while (!Application.loadedLevelName.Equals(levelName))
                yield return null;
        }
    }
}