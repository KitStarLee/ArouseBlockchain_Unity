using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class NeuralNetworComputingModel 
{
    /// <summary>
    /// 定义神经网络类
    /// </summary>
    public class NeuralNetwork
    {
        public int inputs;
        public int hidden;
        public int outputs;

        public Neuron[] inputLayer; //输入层
        public Neuron[] hiddenLayer; //隐藏层
        public Neuron[] outputLayer; //输出层

        public NeuralNetwork(int inputs, int hidden, int outputs)
        {
            this.inputs = inputs;
            this.hidden = hidden;
            this.outputs = outputs;

            inputLayer = new Neuron[inputs];
            for (int i = 0; i < inputs; i++)
            {
                inputLayer[i] = new Neuron(1); //每个输入神经元只有一个输入信号
            }

            hiddenLayer = new Neuron[hidden];
            for (int i = 0; i < hidden; i++)
            {
                hiddenLayer[i] = new Neuron(inputs); //每个隐藏神经元有inputs个输入信号
            }

            outputLayer = new Neuron[outputs];
            for (int i = 0; i < outputs; i++)
            {
                outputLayer[i] = new Neuron(hidden); //每个输出神经元有hidden个输入信号
            }
        }

        public double[] FeedForward(double[] inputs)
        {
            double[] hiddenOutputs = new double[hidden];
            for (int i = 0; i < hidden; i++)
            {
                hiddenOutputs[i] = hiddenLayer[i].FeedForward(inputs); //计算隐藏层的输出值
            }

            double[] outputs = new double[this.outputs];
            for (int i = 0; i < this.outputs; i++)
            {
                outputs[i] = outputLayer[i].FeedForward(hiddenOutputs); //计算输出层的输出值
            }

            return outputs;
        }
    }

    /// <summary>
    /// 定义神经元类
    /// 分为细胞体和突起两部分
    /// </summary>
    public class Neuron
    {
        public double[] weights; //权重
        public double output; //输出值

        public Neuron(int inputs)
        {
            weights = new double[inputs];
            Random rand = new Random();
            for (int i = 0; i < inputs; i++)
            {
                weights[i] = rand.NextDouble() * 2 - 1; //随机初始化权重
            }
        }

        /// <summary>
        /// 前突起
        /// </summary>
        /// <param name="inputs">输入的信息</param>
        /// <returns>返回处理后的信息</returns>
        public double FeedForward(double[] inputs)
        {
            double sum = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                sum += inputs[i] * weights[i]; //计算加权和
            }
            output = Sigmoid(sum); //使用Sigmoid函数进行激活
            return output;
        }

        /// <summary>
        /// 激活处理，隐藏层，简单的加权求求和
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }
    }
}
