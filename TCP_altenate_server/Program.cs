
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace HTTPServer
{
    // Класс-обработчик клиента
    class ClientClass
    {
        int curr_client_num;
        private void SendError(TcpClient Client, string err)
        {
            string Str = "ERROR: " + err;
            // Вывод в консоль сервера всякой хрени
            Console.WriteLine("\nError " + err + " has been sent to the client #" + curr_client_num.ToString() + ".");
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            try
            {
                Client.GetStream().Write(Buffer, 0, Buffer.Length);
            }
            catch (Exception e)
            {

            }
            // Закроем соединение
            Client.Close();
        }

        private void SendMessage(TcpClient Client, string msg)
        {
            // Вывод в консоль сервера всякой хрени
            Console.WriteLine("\nMessage \"" + msg + "\" has been sent to the client #" + curr_client_num.ToString() + ".");
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(msg);
            // Отправим его клиенту
            try
            {
                Client.GetStream().Write(Buffer, 0, Buffer.Length);
            }
            catch (Exception e)
            {

            }
        }

        public ClientClass(TcpClient Client, int num)
        {
            curr_client_num = num;
            // Объявим строку, в которой будет хранится запрос клиента
            string Request = "";
            // Буфер для хранения принятых от клиента данных
            byte[] Buffer = new byte[1024];
            // Переменная для хранения количества байт, принятых от клиента
            int Count;
            // Читаем из потока клиента до тех пор, пока от него поступают данные
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                // Преобразуем эти данные в строку и добавим ее к переменной Request
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                Console.Write("\nClient-"+curr_client_num.ToString()+"@My-Awesome-Server:$ " + Request);
                // SendMessage(Client, "Hello!");
                break;
            }

            // Парсим строку запроса с использованием регулярных выражений
            // При этом отсекаем все переменные GET-запроса
            Match SaveReqMatch = Regex.Match(Request, "save \".+\" to \".+\"");
            Match GetReqMatch = Regex.Match(Request, "give me \".+\" please");

            // Если запрос не удался
            if (SaveReqMatch == Match.Empty && GetReqMatch == Match.Empty)
            {
                // Передаем клиенту ошибку 400 - неверный запрос
                SendError(Client, "400 bad request");
                return;
            }
            if (SaveReqMatch != Match.Empty)
            {
                string[] parts = Request.Split('"');
                string text = parts[1];
                string fileName = parts[3];
                StreamWriter file = new StreamWriter(fileName);
                try
                {
                    file.Write(text);
                }
                catch
                {
                    Console.WriteLine("Oh crap...");
                    File.Create(fileName);
                    file.WriteLine(text);
                }
                Console.WriteLine("Written to "+fileName);
                SendMessage(Client, "done.");
                file.Close();
            }

            else if (GetReqMatch != Match.Empty)
            {
                string[] parts = Request.Split('"');
                string fileName = parts[1];
                try
                {
                    StreamReader file = new StreamReader(fileName);
                    string text = file.ReadToEnd();
                    SendMessage(Client, text);
                    file.Close();
                }
                catch (Exception e)
                {
                    SendError(Client, "no such file exist on the server!");
                }
            }
            //// Посылаем заголовки
            //string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + FS.Length + "\n\n";
            //byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
            //Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);

            //// Пока не достигнут конец файла
            //while (FS.Position < FS.Length)
            //{
            //    // Читаем данные из файла
            //    Count = FS.Read(Buffer, 0, Buffer.Length);
            //    // И передаем их клиенту
            //    Client.GetStream().Write(Buffer, 0, Count);
            //}

            //// Закроем файл и соединение
            //FS.Close();
            Client.Close();
        }
    }

    class Server
    {
        TcpListener Listener; // Объект, принимающий TCP-клиентов
        static int clientNum = 0;
        // Запуск сервера
        public Server(int Port)
        {
            Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port); // Создаем "слушателя" для указанного порта
            Listener.Start(); // Запускаем его
            Console.WriteLine("My-Awesome-Server: awaiting clients.");

            // В бесконечном цикле
            while (true)
            {
                // Принимаем нового клиента
                TcpClient Client = Listener.AcceptTcpClient();
                // Создаем поток
                Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                // И запускаем этот поток, передавая ему принятого клиента
                Thread.Start(Client);
            }
        }

        static void ClientThread(Object StateInfo)
        {
            clientNum++;
            Console.WriteLine("Client " + clientNum.ToString() + " connected.");
            new ClientClass((TcpClient)StateInfo, clientNum);
        }

        // Остановка сервера
        ~Server()
        {
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }

        static void Main(string[] args)
        {
            // Создадим новый сервер на порту 80
            new Server(1234);
        }
    }
}
