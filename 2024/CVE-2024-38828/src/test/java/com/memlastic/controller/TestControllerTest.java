package com.memlastic.controller;

import com.memlastic.exception.DataLimitExceededException;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;

import java.io.InputStream;

import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
class TestControllerTest {
    @Autowired
    private MockMvc mockMvc;

    @Test
    void testUploadTextPlainSuccess() throws Exception {
        mockMvc.perform(post("/upload").content("This text message is designed specifically for the test"))
                .andExpect(status().isOk())
                .andExpect(content().string("Ok"));
    }

    @Test
    void testUploadFileSuccess() throws Exception {
        ClassLoader classLoader = TestControllerTest.class.getClassLoader();
        InputStream inputStream = classLoader.getResourceAsStream("smallFile.json");
        mockMvc.perform(post("/upload").content(inputStream.readAllBytes()))
                .andExpect(status().isOk())
                .andExpect(content().string("Ok"));
    }

    @Test
    void testUploadFileFailed() throws Exception {
        ClassLoader classLoader = TestControllerTest.class.getClassLoader();
        InputStream inputStream = classLoader.getResourceAsStream("bigFile.json");
        mockMvc.perform(post("/upload").content(inputStream.readAllBytes()))
                .andExpect(result -> assertTrue(result.getResolvedException().fillInStackTrace().getCause() instanceof DataLimitExceededException));

    }
}