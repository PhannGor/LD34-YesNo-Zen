﻿using System.Collections;
using Borodar.LD34.Helpers;
using Borodar.LD34.Questions;
using UnityEngine;
using UnityEngine.UI;

namespace Borodar.LD34.Managers
{
    public class SceneManager : Singleton<SceneManager>
    {
        private const float TIME_PER_QUESTION = 6f;

        [Space(10)]
        public Background Background;
        [Space(10)]
        public Text TimeText;
        public Text ScoreText;
        public Text HighscoreText;
        public Text HighscoreToast;
        [Space(10)]
        public Text ComplexityToast;
        public Text QuestionText;
        public Text HintText;
        [Space(10)]
        public ParticleSystem YesParticles;
        public ParticleSystem NoParticles;
        [Space(10)]
        public Color CorrectColor;
        public Color WrongColor;

        private Question _question;
        private bool _isFirstQuestion = true;
        private bool _isQuestionTrue = true;
        private bool _isCheckingAnswer;
        private int _score;
        private float _timeRemaining;
        private int _prevComplexity;

        private bool _highscoreBeaten;

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        public void Start()
        {
            HighscoreToast.canvasRenderer.SetAlpha(0f);
            ComplexityToast.canvasRenderer.SetAlpha(0f);

            var game = GlobalManager.Game;
            if (game.IsFirstRun)
            {
                game.IsFirstRun = false;
            }
            else
            {
                QuestionText.text = "Play again?";
            }

            UpdateHighscore();
        }

        protected void Update()
        {
            if (_isFirstQuestion || _isCheckingAnswer) return;            

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0) _timeRemaining = 0;

            var seconds = _timeRemaining % 60;
            var centiseconds = Mathf.Floor(_timeRemaining * 100) % 100;

            TimeText.gameObject.SetActive(true);
            TimeText.text = string.Format("{0:00} : {1:00}", seconds, centiseconds);

            if (_timeRemaining <= 0 && !_isCheckingAnswer) StartCoroutine(GameOver());
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void GenerateQuestion()
        {
            var complexity = (_score + 5) / 10;
            _timeRemaining = TIME_PER_QUESTION + complexity * 2;

            _question = new Question(complexity);
            _isQuestionTrue = Random.value > 0.5f;

            QuestionText.text = (_isQuestionTrue) ? _question.GetTrueString() : _question.GetFakeString();

            if (_prevComplexity < complexity)
            {
                _prevComplexity = complexity;
                StartCoroutine(ShowToast(ComplexityToast));                
            }
        }

        public void CheckAnswer(bool answer)
        {
            if (_isCheckingAnswer) return;

            if (_isFirstQuestion && !answer)
            {
                Application.Quit();
                return; // for web-player
            }

            var isAnswerCorrect = (answer == _isQuestionTrue);
            if (isAnswerCorrect)
            {
                GlobalManager.Audio.PlayRandomCorrectSound();
                UpdateScore();

                if (_isQuestionTrue)
                {
                    YesParticles.Play();
                }
                else
                {
                    NoParticles.Play();
                }

                StartCoroutine(ShowNextQuestion());
            }
            else
            {
                StartCoroutine(GameOver());
            }

            Background.CrossFadeColor();
            _isFirstQuestion = false;
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void UpdateScore()
        {
            if (_isFirstQuestion) return;

            _score++;

            ScoreText.text = "Score: " + _score.ToString("000");
            ScoreText.gameObject.SetActive(true);

            if (!_highscoreBeaten && _score > GlobalManager.Game.HighScore && GlobalManager.Game.HighScore > 0)
            {
                _highscoreBeaten = true;
                StartCoroutine(ShowToast(HighscoreToast));
            }            
        }

        private void UpdateHighscore()
        {
            var game = GlobalManager.Game;
            if (game.HighScore < _score) game.HighScore = _score;
            if (game.HighScore <= 0) return;

            HighscoreText.text = "Highscore: " + game.HighScore.ToString("000");
            HighscoreText.gameObject.SetActive(true);
        }

        private IEnumerator ShowNextQuestion()
        {
            _isCheckingAnswer = true;
            yield return new WaitForSeconds(1f);

            HintText.gameObject.SetActive(false);
            GenerateQuestion();

            _isCheckingAnswer = false;
        }

        private static IEnumerator ShowToast(Graphic toastText)
        {
            const float duration = 0.75f;
            toastText.CrossFadeAlpha(1f, duration, false);
            yield return new WaitForSeconds(duration + 0.5f);
            toastText.CrossFadeAlpha(0f, duration, false);
        }

        private IEnumerator GameOver()
        {
            _isCheckingAnswer = true;

            GlobalManager.Audio.StopMusic();
            GlobalManager.Audio.PlayWrongSound();

            UpdateHighscore();
            GlobalManager.Game.SaveGameData();

            if (_isQuestionTrue)
            {
                QuestionText.color = CorrectColor;
            }
            else
            {
                QuestionText.color = WrongColor;
                HintText.text = _question.GetTrueString();
                HintText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(4f);

            GlobalManager.Game.LoadScene(Application.loadedLevelName);
        }
    }
}