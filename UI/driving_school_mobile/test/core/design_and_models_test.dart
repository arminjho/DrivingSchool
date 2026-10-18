import 'dart:math' as math;

import 'package:driving_school_mobile/core/design_tokens.dart';
import 'package:driving_school_mobile/core/json_helpers.dart';
import 'package:driving_school_mobile/models/lesson.dart';
import 'package:driving_school_mobile/models/notification_item.dart';
import 'package:driving_school_mobile/models/study_lesson_detail.dart';
import 'package:flutter/painting.dart';
import 'package:flutter_test/flutter_test.dart';

double _contrast(Color a, Color b) {
  double luminance(Color c) {
    double channel(double v) => v <= 0.03928 ? v / 12.92 : math.pow((v + 0.055) / 1.055, 2.4).toDouble();
    return 0.2126 * channel(c.r) + 0.7152 * channel(c.g) + 0.0722 * channel(c.b);
  }

  final l1 = luminance(a);
  final l2 = luminance(b);
  return (math.max(l1, l2) + 0.05) / (math.min(l1, l2) + 0.05);
}

void main() {
  group('dark theme contrast (WCAG AA = 4.5:1 for text)', () {
    const c = AppColors.dark;

    test('body, secondary and muted text are readable on every surface', () {
      for (final surface in [c.canvas, c.surface, c.surfaceMuted, c.surfaceHover]) {
        expect(_contrast(c.textPrimary, surface), greaterThan(7), reason: 'primary on $surface');
        expect(_contrast(c.textSecondary, surface), greaterThan(4.5), reason: 'secondary on $surface');
        expect(_contrast(c.textMuted, surface), greaterThan(4.5), reason: 'muted on $surface');
      }
    });

    test('every status tone is readable on its own soft badge background', () {
      final pairs = {
        'success': (c.success, c.successSoft),
        'warning': (c.warning, c.warningSoft),
        'danger': (c.danger, c.dangerSoft),
        'info': (c.info, c.infoSoft),
        'neutral': (c.neutral, c.neutralSoft),
        'primary': (c.primary, c.primarySoft),
      };

      pairs.forEach((name, pair) => expect(_contrast(pair.$1, pair.$2), greaterThan(4.5), reason: name));
    });

    test('button and selected-navigation text stay readable', () {
      expect(_contrast(c.onPrimary, c.primary), greaterThan(4.5));
      expect(_contrast(c.navItemSelectedText, c.navItemSelected), greaterThan(7));
      expect(_contrast(c.navText, c.navBackground), greaterThan(7));
      expect(_contrast(c.navTextMuted, c.navBackground), greaterThan(4.5));
    });
  });

  group('JSON helpers', () {
    test('a zone-less timestamp from the API is UTC, not local time', () {
      final parsed = parseUtcDateTime('2026-10-16T12:29:14.1234567');

      expect(parsed.isUtc, isTrue);
      expect((parsed.year, parsed.month, parsed.day, parsed.hour, parsed.minute), (2026, 10, 16, 12, 29));
    });

    test('explicit Z and offsets are respected; a bare date is midnight UTC', () {
      expect(parseUtcDateTime('2026-10-16T12:00:00Z'), DateTime.utc(2026, 10, 16, 12));
      expect(parseUtcDateTime('2026-10-16T14:00:00+02:00'), DateTime.utc(2026, 10, 16, 12));
      expect(parseUtcDateTime('2026-01-31'), DateTime.utc(2026, 1, 31));
      expect(parseUtcDateTimeOrNull(null), isNull);
    });
  });

  group('models mirror the backend DTOs', () {
    test('Lesson reads every LessonDto field, including the payment flags and nulls', () {
      final lesson = Lesson.fromJson({
        'id': 5,
        'studentId': 6,
        'studentFullName': 'Adnan Zukić',
        'studentEmail': 'adnan.zukic@email.com',
        'studentPhoneNumber': '+387 61 123 456',
        'instructorId': 8,
        'instructorFullName': 'Damir Kovačević',
        'vehicleId': null,
        'vehicleRegistrationNumber': null,
        'drivingZoneId': 2,
        'drivingZoneName': 'Stari Grad',
        'startsAtUtc': '2026-10-21T10:00:00',
        'endsAtUtc': '2026-10-21T11:00:00',
        'lessonNumber': 32,
        'note': null,
        'status': 'Confirmed',
        'rejectionReason': null,
        'rating': null,
        'instructorNotes': null,
        'startedAt': null,
        'completedAt': null,
        'createdAt': '2026-10-16T12:32:00',
        'requiresPayment': true,
        'isPaid': false,
      });

      expect(lesson.studentFullName, 'Adnan Zukić');
      expect(lesson.vehicleId, isNull);
      expect(lesson.startsAtUtc, DateTime.utc(2026, 10, 21, 10));
      expect(lesson.status, 'Confirmed');
      expect(lesson.requiresPayment, isTrue);
      expect(lesson.isPaid, isFalse);
    });

    test('StudyLessonDetail carries its parent\'s fields and its sections', () {
      final detail = StudyLessonDetail.fromJson({
        'id': 1,
        'title': 'Saobraćajni znakovi',
        'questionAreaId': 1,
        'questionAreaName': 'Saobraćajni znakovi',
        'tags': 'znakovi',
        'validForYear': 2026,
        'readingMinutes': 8,
        'chapterLabel': 'Poglavlje 3.1',
        'sectionCount': 1,
        'sections': [
          {'id': 3, 'studyLessonId': 1, 'title': 'Uvod', 'content': 'Tekst', 'sortOrder': 1},
        ],
      });

      expect(detail.title, 'Saobraćajni znakovi');
      expect(detail.sections.single.title, 'Uvod');
    });

    test('NotificationItem keeps the server\'s UTC timestamp and can be marked read optimistically', () {
      final item = NotificationItem.fromJson({
        'id': 1,
        'title': 'Čas potvrđen',
        'message': 'Vaš zahtjev je potvrđen.',
        'type': 'LessonConfirmed',
        'isRead': false,
        'createdAt': '2026-10-16T12:00:00',
      });

      expect(item.createdAt.isUtc, isTrue);
      expect(item.asRead().isRead, isTrue);
      expect(item.isRead, isFalse);
    });
  });
}
