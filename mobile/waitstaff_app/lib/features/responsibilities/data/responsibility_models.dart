class ResponsibilityCheck {
  final String key, label;
  final List<String> options;
  final List<String> attentionOptions;
  ResponsibilityCheck.fromJson(Map<String, dynamic> json)
    : key = json['key'] as String,
      label = json['label'] as String,
      options = (json['options'] as List).cast<String>(),
      attentionOptions = (json['attentionOptions'] as List? ?? [])
          .cast<String>();
}

class ResponsibilityEvidence {
  final String id, phase;
  ResponsibilityEvidence.fromJson(Map<String, dynamic> json)
    : id = json['id'] as String,
      phase = json['phase'] as String;
}

class ResponsibilityActivity {
  final String id,
      taskName,
      areaName,
      positionName,
      instructions,
      stage,
      photoMode,
      status,
      workDate;
  final DateTime availableAt, dueAt;
  final String? notes;
  final List<ResponsibilityCheck> checklist;
  final Map<String, String> answers;
  final List<ResponsibilityEvidence> evidence;
  bool get isPending => status == 'Pending' || status == 'Overdue';
  bool get isAvailable =>
      isPending && !DateTime.now().toUtc().isBefore(availableAt);
  ResponsibilityActivity.fromJson(Map<String, dynamic> json)
    : id = json['id'] as String,
      taskName = json['taskName'] as String,
      areaName = json['areaName'] as String,
      positionName = json['positionName'] as String,
      instructions = json['instructions'] as String? ?? '',
      stage = json['stage'] as String,
      photoMode = json['photoMode'] as String,
      status = json['status'] as String,
      workDate = json['workDate'] as String,
      availableAt = DateTime.parse(json['availableAtUtc'] as String).toUtc(),
      dueAt = DateTime.parse(json['dueAtUtc'] as String).toUtc(),
      notes = json['notes'] as String?,
      checklist = (json['checklist'] as List? ?? [])
          .map((e) => ResponsibilityCheck.fromJson(e as Map<String, dynamic>))
          .toList(),
      answers = {
        for (final answer in json['answers'] as List? ?? [])
          answer['key'] as String: answer['value'] as String,
      },
      evidence = (json['evidence'] as List? ?? [])
          .map(
            (e) => ResponsibilityEvidence.fromJson(e as Map<String, dynamic>),
          )
          .toList();
}

class ResponsibilityDay {
  final String workDate;
  final String? message;
  final bool offline;
  final List<ResponsibilityActivity> activities;
  ResponsibilityDay.fromJson(Map<String, dynamic> json, {this.offline = false})
    : workDate = json['workDate'] as String,
      message = json['message'] as String?,
      activities = (json['activities'] as List)
          .map(
            (e) => ResponsibilityActivity.fromJson(e as Map<String, dynamic>),
          )
          .toList();
}

class ResponsibilityCapture {
  final String id;
  final DateTime issuedAt, expiresAt;
  ResponsibilityCapture.fromJson(Map<String, dynamic> json)
    : id = json['id'] as String,
      issuedAt = DateTime.parse(json['issuedAtUtc'] as String).toUtc(),
      expiresAt = DateTime.parse(json['expiresAtUtc'] as String).toUtc();
}
