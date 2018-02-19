using System;
using LunaClient.Systems.SettingsSys;

namespace LunaClient.Windows
{
	public class Langs
	{
		public static string PlayerName = "Player Name:";

		public static string Add = "Add";
		public static string Edit = "Edit";
		public static string Connect = "Connect";
		public static string Remove = "Remove";
		public static string Options = "Options"; 
		public static string Servers = "Servers"; 
		public static string Close = "Close";
		public static string CustomServers = "Custom servers:";
		public static string Cancel = "Cancel";
		public static string Disconnect = "Disconnect";

		//Options menu
		public static string PlayerNameColor = "Player Name color: ";
		public static string Random = "Random";
		public static string Set = "Set";
		public static string SetChatKey = "Set chat key";
		public static string PositioningSystem = "Positioning system:";
		public static string Lang = "Language:";
		public static void UpdateLang(){
			if (SettingsSystem.CurrentSettings.Lang == 1) {
				Add = "Добавить";
				Servers = "Сервера";
				Connect = "Подключится";
				Remove = "Удалить";
				Options = "Настройки";
				Close = "Закрыть";
				Cancel = "Отменить";
				Disconnect = "Отключится";
				CustomServers = "Ваши сервера:";
				PlayerName = "Имя Игрока:";

				PlayerNameColor = "Цвет имени: ";
				Random = "Случайно";
				Set = "Принять";
				PositioningSystem = "Система позиционирования:";
				Lang = "Язык:";
			}
			if (SettingsSystem.CurrentSettings.Lang == 0) {
				Add = "Add";
				Servers = "Servers";
				Connect = "Connect";
				Remove = "Remove";
				Options = "Options";
				Close = "Close";
				Cancel = "Cancel";
				Disconnect = "Disconnect";
				CustomServers = "Custom servers:";
				PlayerName = "Player Name:";

				PlayerNameColor = "Player Name color: ";
				Random = "Random";
				Set = "Set";
				PositioningSystem = "Positioning system:";
				Lang = "Language:";
			}
		}
	}
}

