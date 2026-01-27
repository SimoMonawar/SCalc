using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCalc
{
    /// <summary>
    /// The ViewModel acts as the "Decision Maker" for the app.
    /// It receives clicks from the UI (View), processes the logic, 
    /// and asks the Model to do the math.
    /// </summary>
    public partial class MainPageViewModel : ObservableObject
    {
        #region 1. Setup & Properties

        // Reference to our "Math Brain" (The Model). 
        // We delegate the hard math calculations (parsing, PEMDAS) to this separate class.
        private readonly MainPageModel _calculator = new MainPageModel();

        // The top input line (where the user types)
        [ObservableProperty]
        private string _expressionDisplay = string.Empty;

        // The bottom grey preview line (shows the result as you type)
        [ObservableProperty]
        private string _resultDisplay = string.Empty;

        #endregion

        #region 2. Main Logic (Button Clicks)

        /// <summary>
        /// This method is linked to EVERY calculator button click.
        /// The 'buttonText' parameter comes from the XAML (e.g., "7", "+", "AC").
        /// </summary>
        [RelayCommand]
        public void HandleButtonPress(string buttonText)
        {
            // CASE 1: All Clear (Reset everything)
            if (buttonText == "AC")
            {
                ExpressionDisplay = string.Empty;
                ResultDisplay = string.Empty;
                return;
            }

            // CASE 2: Delete (Remove last character)
            if (buttonText == "DE")
            {
                if (!string.IsNullOrEmpty(ExpressionDisplay))
                {
                    ExpressionDisplay = ExpressionDisplay.Substring(0, ExpressionDisplay.Length - 1);
                }
            }

            // CASE 3: Equals (Move result to main display)
            else if (buttonText == "=")
            {
                if (!string.IsNullOrEmpty(ResultDisplay))
                {
                    ExpressionDisplay = ResultDisplay;
                    ResultDisplay = string.Empty; // Clear preview after moving it up
                }
                return;
            }

            // CASE 4: Percentage (Complex logic: 50+10% vs 50*10%)
            else if (buttonText == "%")
            {
                HandlePercentage();
            }

            // CASE 5: Parentheses (Smart Logic for opening/closing)
            else if (buttonText == "( )")
            {
                HandleParentheses();
            }

            // CASE 6: Standard Inputs (Numbers & Operators)
            else
            {
                HandleStandardInput(buttonText);
            }

            // FINAL STEP: Always update the live preview if possible
            CalculateLiveResult();
        }

        #endregion

        #region 3. Logic Helpers (Refactored for Cleanliness)

        private void HandlePercentage()
        {
            // Safety: Don't run if empty or ending in operator
            if (string.IsNullOrEmpty(ExpressionDisplay) || IsOperator(ExpressionDisplay.Last().ToString()))
                return;

            // Step A: Extract the last number typed
            int index = ExpressionDisplay.Length - 1;
            while (index >= 0)
            {
                if (char.IsDigit(ExpressionDisplay[index]) || ExpressionDisplay[index] == '.')
                    index--;
                else
                    break;
            }

            int numberStartIndex = index + 1;
            string numberText = ExpressionDisplay.Substring(numberStartIndex);

            // Step B: Calculate the percentage value
            if (double.TryParse(numberText, out double currentNumber))
            {
                double newValue;

                // Context Check: Are we adding/subtracting? (e.g. 100 + 10%)
                if (numberStartIndex > 0)
                {
                    char op = ExpressionDisplay[numberStartIndex - 1];
                    if (op == '+' || op == '-')
                    {
                        // Calculate % of the TOTAL so far (100 + 10% -> 100 + 10)
                        string leftSide = ExpressionDisplay.Substring(0, numberStartIndex - 1);
                        double baseValue = _calculator.Evaluate(leftSide);
                        newValue = baseValue * (currentNumber / 100.0);
                    }
                    else
                    {
                        // Just convert to decimal (50 * 10% -> 50 * 0.1)
                        newValue = currentNumber / 100.0;
                    }
                }
                else
                {
                    newValue = currentNumber / 100.0;
                }

                // Step C: Replace the old number with the new value
                ExpressionDisplay = ExpressionDisplay.Substring(0, numberStartIndex) + newValue.ToString();
            }
        }

        private void HandleParentheses()
        {
            // Count open parentheses to decide if we should Open or Close
            int openCount = ExpressionDisplay.Count(c => c == '(') - ExpressionDisplay.Count(c => c == ')');
            string lastChar = ExpressionDisplay.Length > 0 ? ExpressionDisplay.Substring(ExpressionDisplay.Length - 1) : "";

            if (string.IsNullOrEmpty(ExpressionDisplay))
            {
                ExpressionDisplay += "("; // Start with Open
            }
            else if (IsOperator(lastChar) || lastChar == "(")
            {
                ExpressionDisplay += "("; // After operator -> Open new group
            }
            else if (openCount > 0 && (char.IsDigit(lastChar[0]) || lastChar == ")"))
            {
                ExpressionDisplay += ")"; // We have an open group & a number -> Close it
            }
            else
            {
                ExpressionDisplay += "*("; // Number but no open group -> Implicit Multiply
            }
        }

        private void HandleStandardInput(string buttonText)
        {
            // Logic for Negative Numbers vs Subtraction
            if (buttonText == "-")
            {
                // Start with negative? "-5"
                if (string.IsNullOrEmpty(ExpressionDisplay))
                {
                    ExpressionDisplay += buttonText;
                    return;
                }
                // Operator before negative? "5 * -3"
                char lastChar = ExpressionDisplay.Last();
                if (IsOperator(lastChar.ToString()))
                {
                    ExpressionDisplay += buttonText;
                    return;
                }
            }

            // Prevent Double Operators (e.g., replacing "+" with "-")
            if (IsOperator(buttonText) && ExpressionDisplay.Length > 0 && IsOperator(ExpressionDisplay.Last().ToString()))
            {
                ExpressionDisplay = ExpressionDisplay.Substring(0, ExpressionDisplay.Length - 1) + buttonText;
            }
            else
            {
                ExpressionDisplay += buttonText;
            }
        }

        private void CalculateLiveResult()
        {
            // Don't calculate if the equation is incomplete (ends in + or -)
            if (string.IsNullOrEmpty(ExpressionDisplay) || IsOperator(ExpressionDisplay.Last().ToString()))
            {
                return;
            }
            try
            {
                double result = _calculator.Evaluate(ExpressionDisplay);
                ResultDisplay = result.ToString();
            }
            catch
            {
                ResultDisplay = "Error";
            }
        }

        // Helper: Defines what counts as a mathematical operator
        // Note: Parentheses are NOT operators in this context.
        private bool IsOperator(string text)
        {
            return text == "+" || text == "-" || text == "×" || text == "÷" || text == "*" || text == "/";
        }

        #endregion

        #region 4. Menu Commands

        [RelayCommand]
        private async Task ShowAbout()
        {
            string version = AppInfo.Current.VersionString;
            string build = AppInfo.Current.BuildString;

            string title = "About SCalc";
            string message = $"SCalc Version {version} ({build})\n\n" +
                             "Made by Husam Monawar\n" +
                             "© 2026 All Rights Reserved.";

            await Shell.Current.DisplayAlert(title, message, "OK");
        }

        #endregion
    }
}