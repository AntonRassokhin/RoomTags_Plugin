using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomTags_Plugin
{
    [Transaction(TransactionMode.Manual)]
    public class RoomTags : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                // создаем список уровней из документа
                List<Level> levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .OfType<Level>()
                    .ToList();

                // объявляем список помещений 
                List<ElementId> rooms;

                // создаем помещения в документе через транзакцию, перебирая уровни
                Transaction transaction1 = new Transaction(doc);
                transaction1.Start("Создание помещений");
                foreach (Level level in levels)
                {
                    rooms = (List<ElementId>)doc.Create.NewRooms2(level);
                }
                transaction1.Commit();

                // отфильтровываем созданные помещения в коллекцию
                FilteredElementCollector filteredRooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms);
                // собираем Id помещений в список
                List<ElementId> roomId = filteredRooms.ToElementIds() as List<ElementId>;

                // размещаем марки помещений через транзакцию, перебирая помещения по Id
                Transaction transaction2 = new Transaction(doc);
                transaction2.Start("Установка марок");
                foreach (ElementId id in roomId)
                {
                    Element element = doc.GetElement(id);
                    Room room = element as Room;
                    string levelName = room.Level.Name.Substring(6);
                    room.Name = $"{levelName}-этаж_{room.Number}-пом.";
                    doc.Create.NewRoomTag(new LinkElementId(id), new UV(0, 0), null);
                }
                transaction2.Commit();

                return Result.Succeeded;
            }

            catch (Exception ex)
            {
                // сообщимть о произошедшей ошибке
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
