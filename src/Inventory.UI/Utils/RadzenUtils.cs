using Radzen.Blazor;

namespace Inventory.UI.Utils
{
    /// <summary>
    /// Утилиты для стандартизации Radzen компонентов
    /// </summary>
    public static class RadzenUtils
    {
        #region Button Styles
        
        /// <summary>
        /// Основной стиль кнопки для важных действий
        /// </summary>
        public static ButtonStyle PrimaryButton => ButtonStyle.Primary;
        
        /// <summary>
        /// Вторичный стиль кнопки для дополнительных действий
        /// </summary>
        public static ButtonStyle SecondaryButton => ButtonStyle.Secondary;
        
        /// <summary>
        /// Стиль кнопки для опасных действий (удаление)
        /// </summary>
        public static ButtonStyle DangerButton => ButtonStyle.Danger;
        
        /// <summary>
        /// Стиль кнопки для успешных действий
        /// </summary>
        public static ButtonStyle SuccessButton => ButtonStyle.Success;
        
        /// <summary>
        /// Стиль кнопки для информационных действий
        /// </summary>
        public static ButtonStyle InfoButton => ButtonStyle.Info;
        
        /// <summary>
        /// Стиль кнопки для предупреждений
        /// </summary>
        public static ButtonStyle WarningButton => ButtonStyle.Warning;
        
        #endregion

        #region Button Sizes
        
        /// <summary>
        /// Стандартный размер кнопки
        /// </summary>
        public static ButtonSize StandardSize => ButtonSize.Medium;
        
        /// <summary>
        /// Маленький размер кнопки для компактных интерфейсов
        /// </summary>
        public static ButtonSize SmallSize => ButtonSize.Small;
        
        /// <summary>
        /// Большой размер кнопки для важных действий
        /// </summary>
        public static ButtonSize LargeSize => ButtonSize.Large;
        
        #endregion

        #region Text Styles
        
        /// <summary>
        /// Заголовок H1
        /// </summary>
        public static TextStyle Header1 => TextStyle.H1;
        
        /// <summary>
        /// Заголовок H2
        /// </summary>
        public static TextStyle Header2 => TextStyle.H2;
        
        /// <summary>
        /// Заголовок H3
        /// </summary>
        public static TextStyle Header3 => TextStyle.H3;
        
        /// <summary>
        /// Заголовок H4
        /// </summary>
        public static TextStyle Header4 => TextStyle.H4;
        
        /// <summary>
        /// Заголовок H5
        /// </summary>
        public static TextStyle Header5 => TextStyle.H5;
        
        /// <summary>
        /// Заголовок H6
        /// </summary>
        public static TextStyle Header6 => TextStyle.H6;
        
        /// <summary>
        /// Основной текст
        /// </summary>
        public static TextStyle Body => TextStyle.Body1;
        
        /// <summary>
        /// Вторичный текст
        /// </summary>
        public static TextStyle Caption => TextStyle.Caption;
        
        #endregion

        #region Text Alignment
        
        /// <summary>
        /// Выравнивание по центру
        /// </summary>
        public static TextAlign CenterAlign => TextAlign.Center;
        
        /// <summary>
        /// Выравнивание по левому краю
        /// </summary>
        public static TextAlign LeftAlign => TextAlign.Left;
        
        /// <summary>
        /// Выравнивание по правому краю
        /// </summary>
        public static TextAlign RightAlign => TextAlign.Right;
        
        /// <summary>
        /// Выравнивание по ширине
        /// </summary>
        public static TextAlign JustifyAlign => TextAlign.Justify;
        
        #endregion

        #region Spacing
        
        /// <summary>
        /// Стандартный отступ
        /// </summary>
        public static string StandardGap => "1rem";
        
        /// <summary>
        /// Маленький отступ
        /// </summary>
        public static string SmallGap => "0.5rem";
        
        /// <summary>
        /// Большой отступ
        /// </summary>
        public static string LargeGap => "2rem";
        
        /// <summary>
        /// Очень маленький отступ
        /// </summary>
        public static string ExtraSmallGap => "0.25rem";
        
        #endregion

        #region Stack Orientation
        
        /// <summary>
        /// Горизонтальная ориентация стека
        /// </summary>
        public static Orientation Horizontal => Orientation.Horizontal;
        
        /// <summary>
        /// Вертикальная ориентация стека
        /// </summary>
        public static Orientation Vertical => Orientation.Vertical;
        
        #endregion

        #region Justify Content
        
        /// <summary>
        /// Выравнивание по началу
        /// </summary>
        public static JustifyContent FlexStart => JustifyContent.FlexStart;
        
        /// <summary>
        /// Выравнивание по центру
        /// </summary>
        public static JustifyContent FlexCenter => JustifyContent.Center;
        
        /// <summary>
        /// Выравнивание по концу
        /// </summary>
        public static JustifyContent FlexEnd => JustifyContent.FlexEnd;
        
        /// <summary>
        /// Равномерное распределение с отступами
        /// </summary>
        public static JustifyContent SpaceBetween => JustifyContent.SpaceBetween;
        
        /// <summary>
        /// Равномерное распределение вокруг элементов
        /// </summary>
        public static JustifyContent SpaceAround => JustifyContent.SpaceAround;
        
        #endregion

        #region Align Items
        
        /// <summary>
        /// Выравнивание элементов по началу
        /// </summary>
        public static AlignItems AlignStart => AlignItems.FlexStart;
        
        /// <summary>
        /// Выравнивание элементов по центру
        /// </summary>
        public static AlignItems AlignCenter => AlignItems.Center;
        
        /// <summary>
        /// Выравнивание элементов по концу
        /// </summary>
        public static AlignItems AlignEnd => AlignItems.FlexEnd;
        
        /// <summary>
        /// Выравнивание элементов по базовой линии
        /// </summary>
        public static AlignItems AlignBaseline => AlignItems.Baseline;
        
        #endregion

        #region Form Field Types
        
        /// <summary>
        /// Тип поля формы - заполненное
        /// </summary>
        public static FormFieldType FilledField => FormFieldType.Filled;
        
        /// <summary>
        /// Тип поля формы - с границей
        /// </summary>
        public static FormFieldType OutlinedField => FormFieldType.Outlined;
        
        /// <summary>
        /// Тип поля формы - подчеркнутое
        /// </summary>
        public static FormFieldType UnderlinedField => FormFieldType.Underlined;
        
        #endregion

        #region Icon Names
        
        /// <summary>
        /// Иконки для стандартных действий
        /// </summary>
        public static class Icons
        {
            public static string Add => "add";
            public static string Edit => "edit";
            public static string Delete => "delete";
            public static string Save => "save";
            public static string Cancel => "cancel";
            public static string Refresh => "refresh";
            public static string Search => "search";
            public static string Filter => "filter";
            public static string Close => "close";
            public static string Check => "check";
            public static string Warning => "warning";
            public static string Error => "error";
            public static string Info => "info";
            public static string Success => "check_circle";
            public static string Person => "person";
            public static string Settings => "settings";
            public static string Home => "home";
            public static string Menu => "menu";
            public static string More => "more_vert";
            public static string Back => "arrow_back";
            public static string Forward => "arrow_forward";
            public static string Up => "arrow_upward";
            public static string Down => "arrow_downward";
            public static string Left => "arrow_back_ios";
            public static string Right => "arrow_forward_ios";
        }
        
        #endregion

        #region Notification Severity
        
        /// <summary>
        /// Успешное уведомление
        /// </summary>
        public static NotificationSeverity Success => NotificationSeverity.Success;
        
        /// <summary>
        /// Информационное уведомление
        /// </summary>
        public static NotificationSeverity Info => NotificationSeverity.Info;
        
        /// <summary>
        /// Предупреждающее уведомление
        /// </summary>
        public static NotificationSeverity Warning => NotificationSeverity.Warning;
        
        /// <summary>
        /// Уведомление об ошибке
        /// </summary>
        public static NotificationSeverity Error => NotificationSeverity.Error;
        
        #endregion

        #region Data Grid Settings
        
        /// <summary>
        /// Стандартный размер страницы для таблиц
        /// </summary>
        public static int StandardPageSize => 10;
        
        /// <summary>
        /// Большой размер страницы для таблиц
        /// </summary>
        public static int LargePageSize => 25;
        
        /// <summary>
        /// Маленький размер страницы для таблиц
        /// </summary>
        public static int SmallPageSize => 5;
        
        /// <summary>
        /// Стандартная высота строки таблицы
        /// </summary>
        public static string StandardRowHeight => "48px";
        
        #endregion

        #region Dialog Settings
        
        /// <summary>
        /// Стандартная ширина диалога
        /// </summary>
        public static string StandardDialogWidth => "500px";
        
        /// <summary>
        /// Большая ширина диалога
        /// </summary>
        public static string LargeDialogWidth => "800px";
        
        /// <summary>
        /// Маленькая ширина диалога
        /// </summary>
        public static string SmallDialogWidth => "300px";
        
        #endregion
    }
}
