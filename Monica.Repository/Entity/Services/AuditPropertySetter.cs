using Monica.Authority.Identity.Abstractions;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Entity.Abstractions.Auditing;
using Monica.Repository.Entity.Utils;

namespace Monica.Repository.Entity.Services;
/// <summary>Applies audit identity and UTC timestamps from the current user and injected clock.</summary>
public class AuditPropertySetter(ICurrentUser currentUser, TimeProvider timeProvider) : IAuditPropertySetter
{
    protected ICurrentUser CurrentUser { get; } = currentUser;

    public virtual void SetCreationProperties(object targetObject)
    {
        SetCreationTime(targetObject);
        SetCreatorId(targetObject);
    }

    public virtual void SetModificationProperties(object targetObject)
    {
        SetLastModificationTime(targetObject);
        SetLastModifierId(targetObject);
    }

    public virtual void SetDeletionProperties(object targetObject)
    {
        SetDeletionTime(targetObject);
        SetDeleterId(targetObject);
    }

    public virtual void IncrementEntityVersionProperty(object targetObject)
    {
        if (targetObject is IHasEntityVersion objectWithEntityVersion)
        {
            ObjectHelper.TrySetProperty(objectWithEntityVersion, x => x.EntityVersion, x => x.EntityVersion + 1);
        }
    }

    protected virtual void SetCreationTime(object targetObject)
    {
        if (targetObject is not IHasCreationTime objectWithCreationTime)
        {
            return;
        }

        if (objectWithCreationTime.CreationTime == default)
        {
            ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.CreationTime, () => timeProvider.GetUtcNow().UtcDateTime);
        }
    }


    protected virtual void SetLastModificationTime(object targetObject)
    {
        if (targetObject is IHasModificationTime objectWithModificationTime)
        {
            ObjectHelper.TrySetProperty(objectWithModificationTime, x => x.LastModificationTime, () => timeProvider.GetUtcNow().UtcDateTime);
        }

    }


    protected virtual void SetDeletionTime(object targetObject)
    {
        if (targetObject is IHasDeletionTime { DeletionTime: null } objectWithDeletionTime)
        {
            ObjectHelper.TrySetProperty(objectWithDeletionTime, x => x.DeletionTime, () => timeProvider.GetUtcNow().UtcDateTime);
        }
    }
    protected virtual void SetLastModifierId(object targetObject)
    {
        if (targetObject is not IHasLastModifier modificationAuditedObject)
        {
            return;
        }

        if (targetObject is IHasLastModifierName modifier)
        {
            ObjectHelper.TrySetProperty(modifier, x => x.LastModifier, () => CurrentUser.Username);
        }

        ObjectHelper.TrySetProperty(modificationAuditedObject, x => x.LastModifierId, () => CurrentUser.Id);
    }

    protected virtual void SetCreatorId(object targetObject)
    {
        if (targetObject is not IHasCreator creatorObject) return;

        if (!string.IsNullOrEmpty(creatorObject.CreatorId))
        {
            return;
        }

        if (targetObject is IHasCreatorName objectWithCreatorName)
        {
            ObjectHelper.TrySetProperty(objectWithCreatorName, x => x.Creator, () => CurrentUser.Username);
        }
        ObjectHelper.TrySetProperty(creatorObject, x => x.CreatorId, () => CurrentUser.Id);
    }
    protected virtual void SetDeleterId(object targetObject)
    {
        if (targetObject is not IHasDeleter deletionAuditedObject)
        {
            return;
        }

        if (!string.IsNullOrEmpty(deletionAuditedObject.DeleterId))
        {
            return;
        }
        // Audit inputs come from scoped identity; stamping must not issue another query on the saving context.
        if (targetObject is IHasDeleterName deleter)
        {
            ObjectHelper.TrySetProperty(deleter, x => x.Deleter, () => CurrentUser.Username);
        }

        ObjectHelper.TrySetProperty(deletionAuditedObject, x => x.DeleterId, () => CurrentUser.Id);
    }
}
