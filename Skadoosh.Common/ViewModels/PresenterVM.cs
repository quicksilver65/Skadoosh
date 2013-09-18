﻿using Skadoosh.Common.DomainModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skadoosh.Common.ViewModels
{

    public class PresenterVM : ViewModelBase
    {
        private ObservableCollection<Survey> surveyCollection;
        private Survey currentSurvey;
        private Question currentQuestion;
        private bool isSurveySelected;
        private bool isQuestionSelected;
        private bool canSetActive;
        private bool canStartSurvey;
        private bool canStopSurvey;

        public bool CanStopSurvey
        {
            get { return canStopSurvey; }
            set { canStopSurvey = value; Notify("CanStopSurvey");}
        }
        
        public bool CanStartSurvey
        {
            get { return canStartSurvey; }
            set { canStartSurvey = value; Notify("CanStartSurvey");}
        }
        
        public bool CanSetActive
        {
            get { return canSetActive; }
            set { canSetActive = value; Notify("CanSetActive");}
        }
        
        public bool IsQuestionSelected
        {
            get { return isQuestionSelected; }
            set { isQuestionSelected = value; Notify("IsQuestionSelected");}
        }
        
        public bool IsSurveySelected
        {
            get { return isSurveySelected; }
            set { isSurveySelected = value; Notify("IsSurveySelected");}
        }
        
        public bool IsLoading { get; set; }
      
        public Question CurrentQuestion
        {
            get { return currentQuestion; }
            set
            {
                currentQuestion = value;
                IsQuestionSelected = value != null ? true : false;
                CanSetActive = (value != null && CurrentSurvey.IsLiveSurvey) ? true : false;
                Notify("CurrentQuestion");
            }
        }
        public Survey CurrentSurvey
        {
            get { return currentSurvey; }
            set
            {
                currentSurvey = value;
                IsSurveySelected = value != null ? true : false;
                CanStartSurvey = (value != null && CurrentSurvey.IsLiveSurvey && !CurrentSurvey.IsActive);
                CanStopSurvey = (value != null && CurrentSurvey.IsLiveSurvey && CurrentSurvey.IsActive);
                Notify("CurrentSurvey");
            }
        }  
        public ObservableCollection<Survey> SurveyCollection
        {
            get { return surveyCollection; }
            set { surveyCollection = value; Notify("SurveyCollection"); }
        }

        public PresenterVM()
        {
            SurveyCollection = new ObservableCollection<Survey>();
        }

        #region Survey Code
        public async void DeleteCurrentSurvey()
        {
            var table = AzureClient.GetTable<Survey>();
            if (CurrentSurvey != null && CurrentSurvey.Id != 0)
            {
                DeleteQuestionBySurvey(currentSurvey.Id);
                await table.DeleteAsync(CurrentSurvey);
                SurveyCollection.Remove(CurrentSurvey);
                CurrentSurvey = null;
            }
        }
        public async void UpdateSurvey()
        {
            var table = AzureClient.GetTable<Survey>();
            if (CurrentSurvey.Id == 0)
            {
                await table.InsertAsync(CurrentSurvey).ContinueWith(x=> LoadSurveysForCurrentUser());
            }
            else
            {
                await table.UpdateAsync(CurrentSurvey).ContinueWith(x => LoadSurveysForCurrentUser());
            }
            

        }
        public async void StartSurvey()
        {
            if (CurrentSurvey.IsLiveSurvey)
            {
                CurrentSurvey.IsActive = true;
                var table = AzureClient.GetTable<Survey>();
                await table.UpdateAsync(CurrentSurvey);
                CanStartSurvey = (CurrentSurvey.IsLiveSurvey && !CurrentSurvey.IsActive);
                CanStopSurvey = (CurrentSurvey.IsLiveSurvey && CurrentSurvey.IsActive);
            }
        }
        public async void StopSurvey()
        {
            if (CurrentSurvey.IsLiveSurvey)
            {
                CurrentSurvey.IsActive = false;
                var table = AzureClient.GetTable<Survey>();
                await table.UpdateAsync(CurrentSurvey);
                CanStartSurvey = (CurrentSurvey.IsLiveSurvey && !CurrentSurvey.IsActive);
                CanStopSurvey = (CurrentSurvey.IsLiveSurvey && CurrentSurvey.IsActive);
            }
        }
        #endregion

        #region Question Code
        public async void DeleteQuestionBySurvey(int surveyId)
        {
            var table = AzureClient.GetTable<Question>();
            var questions = await table.Where(x => x.SurveyId == surveyId).ToListAsync();
            foreach (var q in questions)
            {
                DeleteOptionByQuestionId(q.Id);
                await table.DeleteAsync(q);
            }
        }
        public async void DeleteCurrentQuestion()
        {
            var table = AzureClient.GetTable<Question>();
            DeleteOptionByQuestionId(CurrentQuestion.Id);
            await table.DeleteAsync(CurrentQuestion);
            currentSurvey.Questions.Remove(CurrentQuestion);
            CurrentQuestion = null;
        }
        public async Task<int> UpdateQuestion()
        {
            
            var table = AzureClient.GetTable<Question>();
            var options = AzureClient.GetTable<Option>();
            if (CurrentQuestion.IsNew)
            {
                await table.InsertAsync(CurrentQuestion);
                CurrentSurvey.Questions.Add(CurrentQuestion);
                foreach (var opt in CurrentQuestion.Options)
                {
                    opt.QuestionId = CurrentQuestion.Id;
                }
            }
            else
            {

                await table.UpdateAsync(CurrentQuestion);
                var optTable = AzureClient.GetTable<Option>();
                for (int x = currentQuestion.Options.Count - 1; x > -1; x--)
                {
                    var opt = CurrentQuestion.Options[x];
                    if (opt.IsDeleted)
                    {
                        await optTable.DeleteAsync(opt);
                        CurrentQuestion.Options.Remove(opt);
                    }
                }
            }
            UpdateOptions();
            return CurrentQuestion.Options.Count;
        }
        public async void SetQuestionActive()
        {
            if (CurrentQuestion != null)
            {
                var table = AzureClient.GetTable<Question>();
                var activeQuestion = CurrentSurvey.Questions.FirstOrDefault(x => x.IsActive == true);
                if (activeQuestion != null && activeQuestion.Id != CurrentQuestion.Id)
                {
                    activeQuestion.IsActive = false;
                    CurrentQuestion.IsActive = true;
                    await table.UpdateAsync(activeQuestion);
                    await table.UpdateAsync(CurrentQuestion);
                }
                else
                {
                    CurrentQuestion.IsActive = true;
                    await table.UpdateAsync(CurrentQuestion);
                }
            }
        }
        #endregion

        #region Option Code
        public async void DeleteOptionByQuestionId(int questionId)
        {
            var table = AzureClient.GetTable<Option>();
            var opts = await table.Where(x => x.QuestionId == questionId).ToListAsync();
            foreach (var opt in opts)
            {
                DeleteReponsesByOptionId(opt.Id);
                await table.DeleteAsync(opt);
            }
        }
        public async void UpdateOptions()
        {
            var table = AzureClient.GetTable<Option>();
            foreach (var opt in CurrentQuestion.Options)
            {
                if (opt.QuestionId != 0)
                {
                    if (opt.IsNew)
                    {
                        await table.InsertAsync(opt);
                    }
                    else
                    {
                        await table.UpdateAsync(opt);
                    }
                }
            }

        }  
        #endregion

        #region Response Code
        public async void DeleteReponsesByOptionId(int optionId)
        {
            var table = AzureClient.GetTable<Responses>();
            var responses = await table.Where(x => x.OptionId == optionId).ToListAsync();
            foreach (var response in responses)
            {
                await table.DeleteAsync(response);
            }
        } 
        #endregion


        public async Task<int> LoadSurveysForCurrentUser()
        {
            SurveyCollection.Clear();
            var results = await AzureClient.GetTable<Survey>().Where(x => x.AccountUserId == User.Id).ToListAsync().ConfigureAwait(true);
            foreach (var item in results)
            {
                SurveyCollection.Add(item);
            }
            return SurveyCollection.Count;
        }
        public async Task<int> LoadQuestionsForCurrentSurvey()
        {
            if (CurrentSurvey != null)
            {
                currentSurvey.Questions.Clear();
                var results = await AzureClient.GetTable<Question>().Where(x => x.SurveyId == CurrentSurvey.Id).ToListAsync().ConfigureAwait(true);

                foreach (var q in results)
                {
                    var subResults = await AzureClient.GetTable<Option>().Where(x => x.QuestionId == q.Id).ToListAsync().ConfigureAwait(true);
                    foreach (var o in subResults)
                    {
                        q.Options.Add(o);
                    }
                    currentSurvey.Questions.Add(q);
                }
                return CurrentSurvey.Questions.Count;
            }
            return 0;
        }
    }
}
