using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseProgressView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_progress_view",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_lesson_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'"),
                    completed_lesson_dates = table.Column<List<DateTimeOffset>>(type: "timestamptz[]", nullable: false, defaultValueSql: "'{}'"),
                    completed_lesson_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_lesson_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_progress_view", x => new { x.student_id, x.course_id });
                    table.CheckConstraint("ck_course_progress_view_lesson_arrays_aligned", "cardinality(completed_lesson_ids) = cardinality(completed_lesson_dates)");
                });

            migrationBuilder.Sql(
                """
                INSERT INTO course_progress_view (
                    student_id,
                    course_id,
                    status,
                    started_at,
                    completed_at,
                    completed_lesson_ids,
                    completed_lesson_dates,
                    completed_lesson_count,
                    total_lesson_count)
                SELECT
                    progress.student_id,
                    progress.course_id,
                    progress.status,
                    progress.started_at,
                    progress.completed_at,
                    COALESCE(lessons.lesson_ids, '{}'::uuid[]),
                    COALESCE(lessons.lesson_dates, '{}'::timestamptz[]),
                    cardinality(COALESCE(lessons.lesson_ids, '{}'::uuid[])),
                    NULL
                FROM course_progress AS progress
                LEFT JOIN (
                    SELECT
                        student_id,
                        course_id,
                        array_agg(lesson_id ORDER BY completed_at ASC, lesson_id ASC)
                            AS lesson_ids,
                        array_agg(completed_at ORDER BY completed_at ASC, lesson_id ASC)
                            AS lesson_dates
                    FROM completed_lessons
                    GROUP BY student_id, course_id
                ) AS lessons
                    ON lessons.student_id = progress.student_id
                    AND lessons.course_id = progress.course_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_progress_view");
        }
    }
}
