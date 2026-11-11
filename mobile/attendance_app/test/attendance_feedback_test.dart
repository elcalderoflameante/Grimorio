import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/core/feedback/attendance_feedback.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final messenger =
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;
  final calls = <MethodCall>[];

  setUp(() {
    calls.clear();
    messenger.setMockMethodCallHandler(AttendanceFeedback.channel, (
      call,
    ) async {
      calls.add(call);
      return true;
    });
  });
  tearDown(
    () => messenger.setMockMethodCallHandler(AttendanceFeedback.channel, null),
  );

  test('confirmación, error y prueba usan señales distintas', () async {
    final feedback = AttendanceFeedback();
    await feedback.confirmed();
    await feedback.error();
    await feedback.testSound();
    expect(calls.map((call) => call.method), everyElement('play'));
    expect(calls.map((call) => call.arguments['type']), [
      'confirmed',
      'error',
      'test',
    ]);
  });

  test('un fallo de audio no falla la marcación', () async {
    messenger.setMockMethodCallHandler(AttendanceFeedback.channel, (_) async {
      throw PlatformException(code: 'audio_unavailable');
    });
    await expectLater(AttendanceFeedback().confirmed(), completes);
    await expectLater(AttendanceFeedback().error(), completes);
  });

  test('sin implementación nativa no se propaga una excepción', () async {
    messenger.setMockMethodCallHandler(AttendanceFeedback.channel, null);
    await expectLater(AttendanceFeedback().confirmed(), completes);
  });
}
