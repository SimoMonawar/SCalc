using System;
using System.Collections.Generic;
using System.Linq;

namespace SCalc
{
    /// <summary>
    /// The Model represents the "Brain" of the calculator.
    /// It is Pure Logic: it accepts a string expression (e.g., "5+5") 
    /// and returns a double result (e.g., 10.0).
    /// It has NO knowledge of buttons, colors, or the UI.
    /// </summary>
    public class MainPageModel
    {
        #region 1. Public API

        /// <summary>
        /// The main entry point. Takes a raw string like "5 × (2 + 3)" and solves it.
        /// Handles symbol cleanup and Parentheses Recursion first.
        /// </summary>
        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return 0;

            // Step 1: Sanitize input (Convert UI symbols to Math symbols)
            string mathExpression = expression.Replace("×", "*").Replace("÷", "/");

            // Step 2: Handle Parentheses via Recursion
            // We keep finding the inner-most "(" and solving it until none are left.
            while (mathExpression.Contains("("))
            {
                // Find the LAST open paren (innermost nesting)
                int openIndex = mathExpression.LastIndexOf('(');

                // Find the FIRST closing paren after that open paren
                int closeIndex = mathExpression.IndexOf(')', openIndex);

                // Safety: If user typed "(5+5" without closing it, assume it closes at the end.
                if (closeIndex == -1)
                {
                    closeIndex = mathExpression.Length;
                    mathExpression += ")";
                }

                // Extract the inside part: "5+5" from "(5+5)"
                string innerPart = mathExpression.Substring(openIndex + 1, closeIndex - openIndex - 1);

                // RECURSION: Solve the small part inside the brackets
                double innerResult = EvaluateMathString(innerPart);

                // Replace the whole "(...)" block with the calculated number
                // Remove the old part...
                mathExpression = mathExpression.Remove(openIndex, closeIndex - openIndex + 1);
                // ...Insert the new result
                mathExpression = mathExpression.Insert(openIndex, innerResult.ToString());
            }

            // Step 3: Solve the final parentheses-free expression
            return EvaluateMathString(mathExpression);
        }

        #endregion

        #region 2. Core Parsing Engine

        /// <summary>
        /// Parses a flat string (no parentheses) into a list of numbers and operators.
        /// Example: "-5 + 3 * 2" -> ["-5", "+", "3", "*", "2"]
        /// </summary>
        private double EvaluateMathString(string expression)
        {
            var tokens = new List<string>();
            string currentNumber = ""; // Buffer to build multi-digit numbers (e.g. "1", "0", "5")

            // Loop through every character to separate Numbers from Operators
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                // --- LOGIC: Negative Number vs Subtraction Operator ---
                // We must determine if '-' means "Negative 5" or "Minus 5".
                bool isNegativeSign = false;

                if (c == '-')
                {
                    // It's a negative sign if it appears at the START or after another OPERATOR.
                    bool isStart = (tokens.Count == 0 && string.IsNullOrEmpty(currentNumber));
                    bool followsOp = (tokens.Count > 0 && "+-*/".Contains(tokens.Last()) && string.IsNullOrEmpty(currentNumber));

                    if (isStart || followsOp)
                    {
                        isNegativeSign = true;
                    }
                }

                // Build the Number
                if (char.IsDigit(c) || c == '.' || isNegativeSign)
                {
                    currentNumber += c;
                }
                else
                {
                    // It's an operator! Push the buffered number to the list first.
                    if (!string.IsNullOrEmpty(currentNumber)) tokens.Add(currentNumber);

                    // Push the operator
                    tokens.Add(c.ToString());

                    // Reset buffer
                    currentNumber = "";
                }
            }
            // Catch the final number remaining in the buffer
            if (!string.IsNullOrEmpty(currentNumber)) tokens.Add(currentNumber);

            // --- SOLVER (Order of Operations / PEMDAS) ---

            // Pass 1: Multiplication and Division (High Priority)
            tokens = ProcessOperations(tokens, new[] { "*", "/" });

            // Pass 2: Addition and Subtraction (Low Priority)
            tokens = ProcessOperations(tokens, new[] { "+", "-" });

            // Return Final Result
            if (tokens.Count > 0 && double.TryParse(tokens[0], out double result))
            {
                return result;
            }
            return 0; // Fallback for errors
        }

        #endregion

        #region 3. Math Helpers

        /// <summary>
        /// Scans the token list and solves specific operators.
        /// Reduces the list size as it solves.
        /// Input: ["5", "+", "3", "*", "2"] -> Output after *: ["5", "+", "6"]
        /// </summary>
        private List<string> ProcessOperations(List<string> tokens, string[] targets)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                // Found a target operator? (e.g. "*")
                if (targets.Contains(token))
                {
                    // Ensure we have numbers on both sides
                    if (i > 0 && i < tokens.Count - 1)
                    {
                        double left = double.Parse(tokens[i - 1]);
                        double right = double.Parse(tokens[i + 1]);
                        double result = 0;

                        if (token == "*") result = left * right;
                        if (token == "/") result = left / right;
                        if (token == "+") result = left + right;
                        if (token == "-") result = left - right;

                        // LIST REDUCTION STRATEGY:
                        // 1. Update the Left number with the Result
                        tokens[i - 1] = result.ToString();

                        // 2. Remove the Operator and the Right number
                        tokens.RemoveAt(i);
                        tokens.RemoveAt(i);

                        // 3. Step back the index so we don't skip the next operator
                        i--;
                    }
                }
            }
            return tokens;
        }

        #endregion
    }
}