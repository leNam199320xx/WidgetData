using WidgetData.Application.Interfaces;

namespace WidgetData.Data.Repositories;

public interface IJsonRepository<T> : global::WidgetData.Application.Interfaces.IJsonRepository<T> where T : class { }

public interface IJsonWidgetRepository : global::WidgetData.Application.Interfaces.IJsonWidgetRepository { }

public interface IJsonDataSourceRepository : global::WidgetData.Application.Interfaces.IJsonDataSourceRepository { }

public interface IJsonScheduleRepository : global::WidgetData.Application.Interfaces.IJsonScheduleRepository { }

public interface IJsonExecutionRepository : global::WidgetData.Application.Interfaces.IJsonExecutionRepository { }

public interface IJsonPageRepository : global::WidgetData.Application.Interfaces.IJsonPageRepository { }

public interface IJsonPageVersionRepository : global::WidgetData.Application.Interfaces.IJsonPageVersionRepository { }

public interface IJsonPageWidgetRepository : global::WidgetData.Application.Interfaces.IJsonPageWidgetRepository { }

public interface IJsonWidgetGroupRepository : global::WidgetData.Application.Interfaces.IJsonWidgetGroupRepository { }

public interface IJsonWidgetGroupMemberRepository : global::WidgetData.Application.Interfaces.IJsonWidgetGroupMemberRepository { }

public interface IJsonWidgetConfigArchiveRepository : global::WidgetData.Application.Interfaces.IJsonWidgetConfigArchiveRepository { }

public interface IJsonDeliveryTargetRepository : global::WidgetData.Application.Interfaces.IJsonDeliveryTargetRepository { }

public interface IJsonDeliveryExecutionRepository : global::WidgetData.Application.Interfaces.IJsonDeliveryExecutionRepository { }

public interface IJsonIdeaPostRepository : global::WidgetData.Application.Interfaces.IJsonIdeaPostRepository { }

public interface IJsonIdeaSubscriptionRepository : global::WidgetData.Application.Interfaces.IJsonIdeaSubscriptionRepository { }

public interface IJsonIdeaResultRepository : global::WidgetData.Application.Interfaces.IJsonIdeaResultRepository { }

public interface IJsonFormSubmissionRepository : global::WidgetData.Application.Interfaces.IJsonFormSubmissionRepository { }

public interface IJsonWidgetActivityRepository : global::WidgetData.Application.Interfaces.IJsonWidgetActivityRepository { }
