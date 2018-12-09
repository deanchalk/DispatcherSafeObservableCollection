using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace DispatcherSafeObservableCollectionExample
{
    public partial class MainWindow : Window
    {
        private readonly SafeObservable<TestData> data =
            new SafeObservable<TestData>();

        private readonly Random rand = new Random(DateTime.Now.Millisecond);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            list.DataContext = data;
            var work = new List<Action>();
            for (var i = 0; i < 100; i++)
            {
                work.Add(DoWorkAdd);
                work.Add(DoWorkClear);
                work.Add(DoWorkRemove);
                work.Add(DoWorkRemoveAt);
                work.Add(DoWorkInsert);
                work.Add(DoWorkReplace);
            }

            for (var i = 0; i < 1000; i++)
                work[rand.Next(0, work.Count)]
                    .BeginInvoke(null, null);
        }

        private void DoWorkAdd()
        {
            Thread.Sleep(rand.Next(500, 30000));
            data.Add(new TestData
            {
                Text = $"Thread {Thread.CurrentThread.ManagedThreadId} Added"
            });
        }

        private void DoWorkClear()
        {
            Thread.Sleep(rand.Next(500, 10000));
            data.Clear();
            Debug.WriteLine("Thread {0} Clear", Thread.CurrentThread.ManagedThreadId);
        }

        private void DoWorkRemove()
        {
            Thread.Sleep(rand.Next(500, 10000));
            if (data.Count == 0)
                return;
            var item = data[0];
            data.Remove(item);
            Debug.WriteLine("Thread {0} Remove", Thread.CurrentThread.ManagedThreadId);
        }

        private void DoWorkRemoveAt()
        {
            Thread.Sleep(rand.Next(500, 10000));
            if (data.Count == 0)
                return;
            data.RemoveAt(0);
            Debug.WriteLine("Thread {0} RemoveAt", Thread.CurrentThread.ManagedThreadId);
        }

        private void DoWorkInsert()
        {
            Thread.Sleep(rand.Next(500, 10000));
            data.Insert(rand.Next(0, data.Count), new TestData
            {
                Text = $"Thread {Thread.CurrentThread.ManagedThreadId} Insert"
            });
        }

        private void DoWorkReplace()
        {
            Thread.Sleep(rand.Next(500, 10000));
            data[rand.Next(0, data.Count)] = new TestData
            {
                Text = $"Thread {Thread.CurrentThread.ManagedThreadId} Replace"
            };
        }

        private class TestData
        {
            public string Text { get; set; }
        }
    }
}