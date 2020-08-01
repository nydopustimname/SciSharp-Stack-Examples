﻿/*****************************************************************************
   Copyright 2018 The TensorFlow.NET Authors. All Rights Reserved.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
******************************************************************************/

using NumSharp;
using System;
using Tensorflow;
using Tensorflow.Gradients;
using static Tensorflow.Binding;

namespace TensorFlowNET.Examples
{
    /// <summary>
    /// A linear regression learning algorithm example using TensorFlow library.
    /// https://github.com/aymericdamien/TensorFlow-Examples/blob/master/tensorflow_v2/notebooks/2_BasicModels/linear_regression.ipynb
    /// </summary>
    public class LinearRegression : SciSharpExample, IExample
    {
        int training_steps = 1000;

        // Parameters
        float learning_rate = 0.01f;
        int display_step = 100;

        NumPyRandom rng = np.random;
        NDArray train_X, train_Y;
        int n_samples;

        public ExampleConfig InitConfig()
            => Config = new ExampleConfig
            {
                Name = "Linear Regression",
                Enabled = true,
                IsImportingGraph = false,
                Priority = 4
            };

        public bool Run()
        {
            // tf.compat.v1.disable_eager_execution();

            // Training Data
            PrepareData();

            if (tf.Context.executing_eagerly())
                RunModelInEagerMode();
            else
                BuildModel();
            return true;
        }

        public void RunModelInEagerMode()
        {
            // Set model weights 
            // We can set a fixed init value in order to debug
            // var rnd1 = rng.randn<float>();
            // var rnd2 = rng.randn<float>();
            var W = tf.Variable(-0.06f, name: "weight");
            var b = tf.Variable(-0.73f, name: "bias");
            var optimizer = tf.optimizers.SGD(learning_rate);

            // Run training for the given number of steps.
            foreach (var step in range(1, training_steps + 1))
            {
                // Run the optimization to update W and b values.
                // Wrap computation inside a GradientTape for automatic differentiation.
                using var g = tf.GradientTape();
                // Linear regression (Wx + b).
                var pred = W * train_X + b;
                // Mean square error.
                var loss = tf.reduce_sum(tf.pow(pred - train_Y, 2)) / (2 * n_samples);
                // should stop recording
                // Compute gradients.
                var gradients = g.gradient(loss, (W, b));

                // Update W and b following gradients.
                optimizer.apply_gradients(zip(gradients, (W, b)));

                if (step % display_step == 0)
                {
                    pred = W * train_X + b;
                    loss = tf.reduce_sum(tf.pow(pred - train_Y, 2)) / (2 * n_samples);
                    print($"step: {step}, loss: {loss.numpy()}, W: {W.numpy()}, b: {b.numpy()}");
                }
            }
        }

        public override void BuildModel()
        {
            // tf Graph Input
            var X = tf.placeholder(tf.float32);
            var Y = tf.placeholder(tf.float32);

            // Set model weights 
            // We can set a fixed init value in order to debug
            // var rnd1 = rng.randn<float>();
            // var rnd2 = rng.randn<float>();
            var W = tf.Variable(-0.06f, name: "weight");
            var b = tf.Variable(-0.73f, name: "bias");

            // Construct a linear model
            var pred = tf.add(tf.multiply(X, W), b);

            // Mean squared error
            var cost = tf.reduce_sum(tf.pow(pred - Y, 2.0f)) / (2.0f * n_samples);

            // Gradient descent
            // Note, minimize() knows to modify W and b because Variable objects are trainable=True by default
            var optimizer = tf.train.GradientDescentOptimizer(learning_rate).minimize(cost);

            // Initialize the variables (i.e. assign their default value)
            var init = tf.global_variables_initializer();

            // Start training
            using (var sess = tf.Session())
            {
                // Run the initializer
                sess.run(init);

                // Fit all training data
                for (int epoch = 0; epoch < training_steps; epoch++)
                {
                    foreach (var (x, y) in zip<float>(train_X, train_Y))
                        sess.run(optimizer, (X, x), (Y, y));

                    // Display logs per epoch step
                    if ((epoch + 1) % display_step == 0)
                    {
                        var c = sess.run(cost, (X, train_X), (Y, train_Y));
                        Console.WriteLine($"Epoch: {epoch + 1} cost={c} " + $"W={sess.run(W)} b={sess.run(b)}");
                    }
                }

                Console.WriteLine("Optimization Finished!");
                var training_cost = sess.run(cost, (X, train_X), (Y, train_Y));
                Console.WriteLine($"Training cost={training_cost} W={sess.run(W)} b={sess.run(b)}");

                // Testing example
                var test_X = np.array(6.83f, 4.668f, 8.9f, 7.91f, 5.7f, 8.7f, 3.1f, 2.1f);
                var test_Y = np.array(1.84f, 2.273f, 3.2f, 2.831f, 2.92f, 3.24f, 1.35f, 1.03f);
                Console.WriteLine("Testing... (Mean square loss Comparison)");
                var testing_cost = sess.run(tf.reduce_sum(tf.pow(pred - Y, 2.0f)) / (2.0f * test_X.shape[0]),
                    (X, test_X), (Y, test_Y));
                Console.WriteLine($"Testing cost={testing_cost}");
                var diff = Math.Abs((float)training_cost - (float)testing_cost);
                Console.WriteLine($"Absolute mean square loss difference: {diff}");
            }
        }
        public override void PrepareData()
        {
            train_X = np.array(3.3f, 4.4f, 5.5f, 6.71f, 6.93f, 4.168f, 9.779f, 6.182f, 7.59f, 2.167f,
             7.042f, 10.791f, 5.313f, 7.997f, 5.654f, 9.27f, 3.1f);
            train_Y = np.array(1.7f, 2.76f, 2.09f, 3.19f, 1.694f, 1.573f, 3.366f, 2.596f, 2.53f, 1.221f,
                         2.827f, 3.465f, 1.65f, 2.904f, 2.42f, 2.94f, 1.3f);
            n_samples = train_X.shape[0];
        }
    }
}
